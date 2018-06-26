using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.ExportImport.PushNotifications;
using VirtoCommerce.Platform.Core.Extensions;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Web.Extensions;
using VirtoCommerce.Platform.Web.Hangfire;

using Permissions = VirtoCommerce.Platform.Core.PlatformConstants.Security.Permissions;

namespace VirtoCommerce.Platform.Web.Controllers.Api
{
    [Route("api/platform")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PlatformExportImportController : Controller
    {
        private const string _sampledataStateSetting = "VirtoCommerce.SampleDataState";
        private static string _stringSampleDataUrl;

        private readonly IPlatformExportImportManager _platformExportManager;
        private readonly IPushNotificationManager _pushNotifier;
        private readonly IBlobStorageProvider _blobStorageProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ISettingsManager _settingsManager;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IHostingEnvironment _hostEnv;
        private readonly PlatformOptions _platformOptions;

        private static readonly object _lockObject = new object();

        public PlatformExportImportController(IPlatformExportImportManager platformExportManager, IPushNotificationManager pushNotifier, IBlobStorageProvider blobStorageProvider, IBlobUrlResolver blobUrlResolver,
            ISettingsManager settingManager, IUserNameResolver userNameResolver, IHostingEnvironment hostingEnvironment, IOptions<PlatformOptions> options)
        {
            _platformExportManager = platformExportManager;
            _pushNotifier = pushNotifier;
            _blobStorageProvider = blobStorageProvider;
            _blobUrlResolver = blobUrlResolver;
            _settingsManager = settingManager;
            _userNameResolver = userNameResolver;
            _hostEnv = hostingEnvironment;
            _platformOptions = options.Value;

            _stringSampleDataUrl = options.Value.SampleDataUrl;
        }

        [HttpGet]
        [Route("sampledata/discover")]
        [ProducesResponseType(typeof(SampleDataInfo[]), 200)]
        [AllowAnonymous]
        public IActionResult DiscoverSampleData()
        {
            return Ok(InnerDiscoverSampleData().ToArray());
        }

        [HttpPost]
        [Route("sampledata/autoinstall")]
        [ProducesResponseType(typeof(SampleDataImportPushNotification), 200)]
        [Authorize(Permissions.PlatformImport)]
        public IActionResult TryToAutoInstallSampleData()
        {
            var sampleData = InnerDiscoverSampleData().FirstOrDefault(x => !x.Url.IsNullOrEmpty());
            if (sampleData != null)
            {
                return ImportSampleData(sampleData.Url);
            }

            return NoContent();
        }

        [HttpPost]
        [Route("sampledata/import")]
        [ProducesResponseType(typeof(SampleDataImportPushNotification), 200)]
        [Authorize(Permissions.PlatformImport)]
        public IActionResult ImportSampleData([FromQuery]string url = null)
        {
            lock (_lockObject)
            {
                var sampleDataState = EnumUtility.SafeParse(_settingsManager.GetValue(_sampledataStateSetting, SampleDataState.Undefined.ToString()), SampleDataState.Undefined);
                if (sampleDataState == SampleDataState.Undefined)
                {
                    //Sample data initialization
                    if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    {
                        _settingsManager.SetValue(_sampledataStateSetting, SampleDataState.Processing);
                        var pushNotification = new SampleDataImportPushNotification(User.Identity.Name);
                        _pushNotifier.Send(pushNotification);
                        var jobId = BackgroundJob.Enqueue(() => SampleDataImportBackground(new Uri(url), _hostEnv.MapPath(_platformOptions.LocalUploadFolderPath), pushNotification, JobCancellationToken.Null, null));

                        pushNotification.JobId = jobId;

                        return Ok(pushNotification);
                    }
                }
            }

            return NoContent();
        }

        /// <summary>
        /// This method used for azure automatically deployment
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("sampledata/state")]
        [ProducesResponseType(typeof(SampleDataState), 200)]
        [ApiExplorerSettings(IgnoreApi = true)]
        [AllowAnonymous]
        public IActionResult GetSampleDataState()
        {
            var state = EnumUtility.SafeParse(_settingsManager.GetValue(_sampledataStateSetting, string.Empty), SampleDataState.Undefined);
            return Ok(state);
        }

        [HttpGet]
        [Route("export/manifest/new")]
        [ProducesResponseType(typeof(PlatformExportManifest), 200)]
        public IActionResult GetNewExportManifest()
        {
            return Ok(_platformExportManager.GetNewExportManifest(_userNameResolver.GetCurrentUserName()));
        }

        [HttpGet]
        [Route("export/manifest/load")]
        [ProducesResponseType(typeof(PlatformExportManifest), 200)]
        public IActionResult LoadExportManifest([FromQuery]string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                throw new ArgumentNullException(nameof(fileUrl));
            }

            var localPath = Path.Combine(Path.GetFullPath(_platformOptions.LocalUploadFolderPath), fileUrl);

            PlatformExportManifest retVal;
            using (var stream = new FileStream(localPath, FileMode.Open))
            {
                retVal = _platformExportManager.ReadExportManifest(stream);
            }
            return Ok(retVal);
        }

        [HttpPost]
        [Route("export")]
        [ProducesResponseType(typeof(PlatformExportPushNotification), 200)]
        [Authorize(Permissions.PlatformImport)]
        public IActionResult ProcessExport([FromBody]PlatformImportExportRequest exportRequest)
        {
            var notification = new PlatformExportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Platform export task",
                Description = "starting export...."
            };
            _pushNotifier.Send(notification);

            var jobId = BackgroundJob.Enqueue(() => PlatformExportBackground(exportRequest, notification, JobCancellationToken.Null, null));
            notification.JobId = jobId;
            return Ok(notification);
        }

        [HttpPost]
        [Route("import")]
        [ProducesResponseType(typeof(PlatformImportPushNotification), 200)]
        [Authorize(Permissions.PlatformImport)]
        public IActionResult ProcessImport([FromBody]PlatformImportExportRequest importRequest)
        {
            var notification = new PlatformImportPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Platform import task",
                Description = "starting import...."
            };
            _pushNotifier.Send(notification);

            var jobId = BackgroundJob.Enqueue(() => PlatformImportBackground(importRequest, notification, JobCancellationToken.Null, null));
            notification.JobId = jobId;

            return Ok(notification);
        }

        [HttpPost]
        [Route("exortimport/tasks/{jobId}/cancel")]
        public IActionResult Cancel([FromRoute]string jobId)
        {
            BackgroundJob.Delete(jobId);
            return Ok();
        }

        private static IEnumerable<SampleDataInfo> InnerDiscoverSampleData()
        {
            var retVal = new List<SampleDataInfo>();

            var sampleDataUrl = _stringSampleDataUrl;
            if (!string.IsNullOrEmpty(sampleDataUrl))
            {
                //Discovery mode
                if (!sampleDataUrl.EndsWith(".zip"))
                {
                    var manifestUrl = sampleDataUrl + "\\manifest.json";
                    using (var client = new WebClient())
                    using (var stream = client.OpenRead(new Uri(manifestUrl)))
                    {
                        //Add empty template
                        retVal.Add(new SampleDataInfo { Name = "Empty" });
                        var sampleDataInfos = stream.DeserializeJson<List<SampleDataInfo>>();
                        //Need filter unsupported versions and take one most new sample data
                        sampleDataInfos = sampleDataInfos.Select(x => new { Version = SemanticVersion.Parse(x.PlatformVersion), x.Name, Data = x })
                                                         .Where(x => x.Version.IsCompatibleWith(PlatformVersion.CurrentVersion))
                                                         .GroupBy(x => x.Name)
                                                         .Select(x => x.OrderByDescending(y => y.Version).First().Data)
                                                         .ToList();
                        //Convert relative  sample data urls to absolute
                        foreach (var sampleDataInfo in sampleDataInfos)
                        {
                            if (!Uri.IsWellFormedUriString(sampleDataInfo.Url, UriKind.Absolute))
                            {
                                var uri = new Uri(sampleDataUrl);
                                sampleDataInfo.Url = new Uri(uri, uri.AbsolutePath + "/" + sampleDataInfo.Url).ToString();
                            }
                        }
                        retVal.AddRange(sampleDataInfos);

                    }
                }
                else
                {
                    //Direct file mode
                    retVal.Add(new SampleDataInfo { Url = sampleDataUrl });
                }
            }
            return retVal;
        }


        public void SampleDataImportBackground(Uri url, string tmpPath, SampleDataImportPushNotification pushNotification, IJobCancellationToken cancellationToken, PerformContext context)
        {
            Action<ExportImportProgressInfo> progressCallback = x =>
            {
                pushNotification.Path(x);
                pushNotification.JobId = context.BackgroundJob.Id;
                _pushNotifier.Send(pushNotification);
            };

            try
            {
                var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);

                pushNotification.Description = "Start downloading from " + url;
                _pushNotifier.Send(pushNotification);

                if (!Directory.Exists(tmpPath))
                {
                    Directory.CreateDirectory(tmpPath);
                }

                var tmpFilePath = Path.Combine(tmpPath, Path.GetFileName(url.ToString()));
                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += (sender, args) =>
                    {
                        pushNotification.Description = string.Format("Sample data {0} of {1} downloading...", args.BytesReceived.ToHumanReadableSize(), args.TotalBytesToReceive.ToHumanReadableSize());
                        _pushNotifier.Send(pushNotification);
                    };
                    var task = client.DownloadFileTaskAsync(url, tmpFilePath);
                    task.Wait();
                }
                using (var stream = new FileStream(tmpFilePath, FileMode.Open))
                {
                    var manifest = _platformExportManager.ReadExportManifest(stream);
                    if (manifest != null)
                    {
                        _platformExportManager.ImportAsync(stream, manifest, progressCallback, cancellationTokenWrapper);
                    }
                }
            }
            catch (JobAbortedException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                pushNotification.Errors.Add(ex.ExpandExceptionMessage());
            }
            finally
            {
                _settingsManager.SetValue(_sampledataStateSetting, SampleDataState.Completed);
                pushNotification.Description = "Import finished";
                pushNotification.Finished = DateTime.UtcNow;
                _pushNotifier.Send(pushNotification);
            }
        }

        public void PlatformImportBackground(PlatformImportExportRequest importRequest, PlatformImportPushNotification pushNotification, IJobCancellationToken cancellationToken, PerformContext context)
        {
            Action<ExportImportProgressInfo> progressCallback = x =>
            {
                pushNotification.Path(x);
                pushNotification.JobId = context.BackgroundJob.Id;
                _pushNotifier.SendAsync(pushNotification);
            };

            var now = DateTime.UtcNow;
            try
            {
                var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);

                var localPath = _hostEnv.MapPath(importRequest.FileUrl);

                //Load source data only from local file system 
                using (var stream = new FileStream(localPath, FileMode.Open))
                {
                    var manifest = importRequest.ToManifest();
                    manifest.Created = now;
                    _platformExportManager.ImportAsync(stream, manifest, progressCallback, cancellationTokenWrapper);
                }
            }
            catch (JobAbortedException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                pushNotification.Errors.Add(ex.ExpandExceptionMessage());
            }
            finally
            {
                pushNotification.Description = "Import finished";
                pushNotification.Finished = DateTime.UtcNow;
                _pushNotifier.Send(pushNotification);
            }
        }

        public void PlatformExportBackground(PlatformImportExportRequest exportRequest, PlatformExportPushNotification pushNotification, IJobCancellationToken cancellationToken, PerformContext context)
        {
            Action<ExportImportProgressInfo> progressCallback = x =>
            {
                pushNotification.Path(x);
                pushNotification.JobId = context.BackgroundJob.Id;
                _pushNotifier.SendAsync(pushNotification);
            };

            try
            {
                var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);

                const string relativeUrl = "tmp/exported_data.zip";
                var localTmpFolder = _hostEnv.MapPath("~/App_Data/Uploads/tmp");
                var localTmpPath = Path.Combine(localTmpFolder, "exported_data.zip");
                if (!Directory.Exists(localTmpFolder))
                {
                    Directory.CreateDirectory(localTmpFolder);
                }
                //Import first to local tmp folder because Azure blob storage doesn't support some special file access mode 
                using (var stream = System.IO.File.Open(localTmpPath, FileMode.OpenOrCreate))
                {
                    var manifest = exportRequest.ToManifest();
                    _platformExportManager.ExportAsync(stream, manifest, progressCallback, cancellationTokenWrapper);
                }
                //Copy export data to blob provider for get public download url
                using (var localStream = System.IO.File.Open(localTmpPath, FileMode.Open))
                using (var blobStream = _blobStorageProvider.OpenWrite(relativeUrl))
                {
                    localStream.CopyTo(blobStream);
                    //Get a download url
                    pushNotification.DownloadUrl = _blobUrlResolver.GetAbsoluteUrl(relativeUrl);
                }
            }
            catch (JobAbortedException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                pushNotification.Errors.Add(ex.ExpandExceptionMessage());
            }
            finally
            {
                pushNotification.Description = "Export finished";
                pushNotification.Finished = DateTime.UtcNow;
                _pushNotifier.Send(pushNotification);
            }
        }
    }
}