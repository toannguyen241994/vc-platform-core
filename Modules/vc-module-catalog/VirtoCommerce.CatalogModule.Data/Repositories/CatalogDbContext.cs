using System;
using System.Collections.Generic;
using System.Text;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CatalogModule.Data.Model;

namespace VirtoCommerce.CatalogModule.Data.Repositories
{
    public class CatalogDbContext : DbContextWithTriggers
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Catalog
            modelBuilder.Entity<CatalogEntity>().ToTable("Catalog").HasKey(x => x.Id);
            modelBuilder.Entity<CatalogEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<CatalogEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<CatalogEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);
            #endregion

            #region Category
            modelBuilder.Entity<CategoryEntity>().ToTable("Category").HasKey(x => x.Id);
            modelBuilder.Entity<CategoryEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<CategoryEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<CategoryEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<CategoryEntity>().HasOne(x => x.ParentCategory)
                         .WithMany().HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryEntity>().HasOne(x => x.Catalog).WithMany().HasForeignKey(x => x.CatalogId)
                        .IsRequired().OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region Item
            modelBuilder.Entity<ItemEntity>().ToTable("Item").HasKey(x => x.Id);
            modelBuilder.Entity<ItemEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<ItemEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<ItemEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<ItemEntity>().HasOne(m => m.Catalog).WithMany().HasForeignKey(x => x.CatalogId)
                        .IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemEntity>().HasOne(m => m.Category).WithMany().HasForeignKey(x => x.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ItemEntity>().HasOne(m => m.Parent).WithMany(x => x.Childrens).HasForeignKey(x => x.ParentId)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ItemEntity>().HasIndex(x => new { x.Code })
                      .IsUnique(true);

            modelBuilder.Entity<ItemEntity>().HasIndex(x => new { x.CatalogId, x.ParentId })
                      .IsUnique(false)
                      .HasName("IX_CatalogId_ParentId");

            #endregion

            #region Property
            modelBuilder.Entity<PropertyEntity>().ToTable("Property").HasKey(x => x.Id);
            modelBuilder.Entity<PropertyEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<PropertyEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<PropertyEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<PropertyEntity>().HasOne(m => m.Catalog).WithMany(x => x.Properties).HasForeignKey(x => x.CatalogId)
                        .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PropertyEntity>().HasOne(m => m.Category).WithMany(x => x.Properties).HasForeignKey(x => x.CategoryId)
                        .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region PropertyDictionaryValue
            modelBuilder.Entity<PropertyDictionaryValueEntity>().ToTable("PropertyDictionaryValue").HasKey(x => x.Id);
            modelBuilder.Entity<PropertyDictionaryValueEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<PropertyDictionaryValueEntity>().HasOne(m => m.Property).WithMany(x => x.DictionaryValues)
                        .HasForeignKey(x => x.PropertyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region PropertyAttribute
            modelBuilder.Entity<PropertyAttributeEntity>().ToTable("PropertyAttribute").HasKey(x => x.Id);
            modelBuilder.Entity<PropertyAttributeEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<PropertyAttributeEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<PropertyAttributeEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<PropertyAttributeEntity>().HasOne(m => m.Property).WithMany(x => x.PropertyAttributes).HasForeignKey(x => x.PropertyId)
                        .IsRequired().OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region PropertyDisplayName
            modelBuilder.Entity<PropertyDisplayNameEntity>().ToTable("PropertyDisplayName").HasKey(x => x.Id);
            modelBuilder.Entity<PropertyDisplayNameEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<PropertyDisplayNameEntity>().HasOne(m => m.Property).WithMany(x => x.DisplayNames)
                        .HasForeignKey(x => x.PropertyId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region PropertyValue
            modelBuilder.Entity<PropertyValueEntity>().ToTable("PropertyValue").HasKey(x => x.Id);
            modelBuilder.Entity<PropertyValueEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<PropertyValueEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<PropertyValueEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<PropertyValueEntity>().HasOne(m => m.CatalogItem).WithMany(x => x.ItemPropertyValues)
                        .HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PropertyValueEntity>().HasOne(m => m.Category).WithMany(x => x.CategoryPropertyValues)
                        .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PropertyValueEntity>().HasOne(m => m.Catalog).WithMany(x => x.CatalogPropertyValues)
                        .HasForeignKey(x => x.CatalogId).OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region PropertyValidationRule
            modelBuilder.Entity<PropertyValidationRuleEntity>().ToTable("PropertyValidationRule").HasKey(x => x.Id);
            modelBuilder.Entity<PropertyValidationRuleEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<PropertyValidationRuleEntity>().HasOne(m => m.Property).WithMany(x => x.ValidationRules).HasForeignKey(x => x.PropertyId)
                        .IsRequired().OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region CatalogImage
            modelBuilder.Entity<ImageEntity>().ToTable("CatalogImage").HasKey(x => x.Id);
            modelBuilder.Entity<ImageEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<ImageEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<ImageEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<ImageEntity>().HasOne(m => m.Category).WithMany(x => x.Images)
                        .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict); 
            modelBuilder.Entity<ImageEntity>().HasOne(m => m.CatalogItem).WithMany(x => x.Images)
                        .HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region EditorialReview
            modelBuilder.Entity<EditorialReviewEntity>().ToTable("EditorialReview").HasKey(x => x.Id);
            modelBuilder.Entity<EditorialReviewEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<EditorialReviewEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<EditorialReviewEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<EditorialReviewEntity>().HasOne(x => x.CatalogItem).WithMany(x => x.EditorialReviews)
                        .HasForeignKey(x => x.ItemId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region Association
            modelBuilder.Entity<AssociationEntity>().ToTable("Association").HasKey(x => x.Id);
            modelBuilder.Entity<AssociationEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<AssociationEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<AssociationEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<AssociationEntity>().HasOne(m => m.Item).WithMany(x => x.Associations)
                        .HasForeignKey(x => x.ItemId).IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AssociationEntity>().HasOne(a => a.AssociatedItem).WithMany(i => i.ReferencedAssociations)
                        .HasForeignKey(a => a.AssociatedItemId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AssociationEntity>().HasOne(a => a.AssociatedCategory).WithMany()
                        .HasForeignKey(a => a.AssociatedCategoryId).OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Asset
            modelBuilder.Entity<AssetEntity>().ToTable("CatalogAsset").HasKey(x => x.Id);
            modelBuilder.Entity<AssetEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<AssetEntity>().Property(x => x.CreatedBy).HasMaxLength(64);
            modelBuilder.Entity<AssetEntity>().Property(x => x.ModifiedBy).HasMaxLength(64);

            modelBuilder.Entity<AssetEntity>().HasOne(m => m.CatalogItem).WithMany(x => x.Assets).HasForeignKey(x => x.ItemId)
                        .IsRequired().OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region CatalogLanguage
            modelBuilder.Entity<CatalogLanguageEntity>().ToTable("CatalogLanguage").HasKey(x => x.Id);
            modelBuilder.Entity<CatalogLanguageEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<CatalogLanguageEntity>().HasOne(m => m.Catalog).WithMany(x => x.CatalogLanguages)
                        .HasForeignKey(x => x.CatalogId).IsRequired().OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region CategoryItemRelation
            modelBuilder.Entity<CategoryItemRelationEntity>().ToTable("CategoryItemRelation").HasKey(x => x.Id);
            modelBuilder.Entity<CategoryItemRelationEntity>().Property(x => x.Id).HasMaxLength(128);
            modelBuilder.Entity<CategoryItemRelationEntity>().HasOne(p => p.Category).WithMany()
                        .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryItemRelationEntity>().HasOne(p => p.CatalogItem).WithMany(x => x.CategoryLinks).HasForeignKey(x => x.ItemId)
                        .IsRequired().OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CategoryItemRelationEntity>().HasOne(p => p.Catalog).WithMany().HasForeignKey(x => x.CatalogId)
                        .IsRequired().OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region CategoryRelation
            modelBuilder.Entity<CategoryRelationEntity>().ToTable("CategoryRelation").HasKey(x => x.Id);
            modelBuilder.Entity<CategoryRelationEntity>().Property(x => x.Id).HasMaxLength(128);

            modelBuilder.Entity<CategoryRelationEntity>().HasOne(x => x.TargetCategory).WithMany(x => x.IncommingLinks)
                        .HasForeignKey(x => x.TargetCategoryId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategoryRelationEntity>().HasOne(x => x.SourceCategory).WithMany(x => x.OutgoingLinks)
                        .HasForeignKey(x => x.SourceCategoryId).IsRequired().OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategoryRelationEntity>().HasOne(x => x.TargetCatalog)
                        .WithMany(x => x.IncommingLinks)
                        .HasForeignKey(x => x.TargetCatalogId).OnDelete(DeleteBehavior.Restrict);
            #endregion

         
            base.OnModelCreating(modelBuilder);
        }

    }
}
