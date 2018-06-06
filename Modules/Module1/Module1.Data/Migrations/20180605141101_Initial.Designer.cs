﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Module1.Data;

namespace Module1.Data.Migrations
{
    [DbContext(typeof(PlatformDbContext2))]
    [Migration("20180605141101_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-rtm-30799")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("VirtoCommerce.Platform.Data.Model.SettingEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64);

                    b.Property<DateTime>("CreatedDate");

                    b.Property<string>("Description")
                        .HasMaxLength(1024);

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.Property<bool>("IsEnum");

                    b.Property<bool>("IsLocaleDependant");

                    b.Property<bool>("IsMultiValue");

                    b.Property<bool>("IsSystem");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64);

                    b.Property<DateTime?>("ModifiedDate");

                    b.Property<string>("Name")
                        .HasMaxLength(128);

                    b.Property<string>("ObjectId")
                        .HasMaxLength(128);

                    b.Property<string>("ObjectType")
                        .HasMaxLength(128);

                    b.Property<string>("SettingValueType")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.ToTable("SettingEntity");

                    b.HasDiscriminator<string>("Discriminator").HasValue("SettingEntity");
                });

            modelBuilder.Entity("VirtoCommerce.Platform.Data.Model.SettingValueEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("BooleanValue");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64);

                    b.Property<DateTime>("CreatedDate");

                    b.Property<DateTime?>("DateTimeValue");

                    b.Property<decimal>("DecimalValue");

                    b.Property<int>("IntegerValue");

                    b.Property<string>("Locale")
                        .HasMaxLength(64);

                    b.Property<string>("LongTextValue");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64);

                    b.Property<DateTime?>("ModifiedDate");

                    b.Property<string>("SettingId");

                    b.Property<string>("ShortTextValue")
                        .HasMaxLength(512);

                    b.Property<string>("ValueType")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.HasIndex("SettingId");

                    b.ToTable("SettingValueEntity");
                });

            modelBuilder.Entity("Module1.Data.SettingEntity2", b =>
                {
                    b.HasBaseType("VirtoCommerce.Platform.Data.Model.SettingEntity");

                    b.Property<string>("NewField");

                    b.ToTable("PlatformSetting");

                    b.HasDiscriminator().HasValue("SettingEntity2");
                });

            modelBuilder.Entity("VirtoCommerce.Platform.Data.Model.SettingValueEntity", b =>
                {
                    b.HasOne("VirtoCommerce.Platform.Data.Model.SettingEntity", "Setting")
                        .WithMany("SettingValues")
                        .HasForeignKey("SettingId");
                });
#pragma warning restore 612, 618
        }
    }
}
