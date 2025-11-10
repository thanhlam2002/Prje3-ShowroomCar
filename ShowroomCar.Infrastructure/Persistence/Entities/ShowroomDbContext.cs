using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class ShowroomDbContext : DbContext
{
    public ShowroomDbContext()
    {
    }

    public ShowroomDbContext(DbContextOptions<ShowroomDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Allotment> Allotments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Document> Documents { get; set; }

    public virtual DbSet<DocumentLink> DocumentLinks { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }

    public virtual DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }

    public virtual DbSet<InventoryMove> InventoryMoves { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PaymentAllocation> PaymentAllocations { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public virtual DbSet<Quotation> Quotations { get; set; }

    public virtual DbSet<QuotationItem> QuotationItems { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<SalesOrderItem> SalesOrderItems { get; set; }

    public virtual DbSet<ServiceOrder> ServiceOrders { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleImage> VehicleImages { get; set; }

    public virtual DbSet<VehicleModel> VehicleModels { get; set; }

    public virtual DbSet<VehicleRegistration> VehicleRegistrations { get; set; }

    public virtual DbSet<Waitlist> Waitlists { get; set; }

    public virtual DbSet<WaitlistEntry> WaitlistEntries { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public DbSet<GoodsReturn> GoodsReturns { get; set; }

    public DbSet<GoodsReturnItem> GoodsReturnItems { get; set; }

    // Connection string được cấu hình qua DI trong Program.cs
    // Không cần OnConfiguring nữa vì đã có DbContextOptions được inject

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Allotment>(entity =>
        {
            entity.HasKey(e => e.AllotId).HasName("PRIMARY");

            entity
                .ToTable("allotments")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.SoId, "fk_allot_so");

            entity.HasIndex(e => e.VehicleId, "vehicle_id").IsUnique();

            entity.Property(e => e.AllotId)
                .HasColumnType("bigint(20)")
                .HasColumnName("allot_id");
            entity.Property(e => e.ReservedAt)
                .HasColumnType("datetime")
                .HasColumnName("reserved_at");
            entity.Property(e => e.SoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("so_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.So).WithMany(p => p.Allotments)
                .HasForeignKey(d => d.SoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_allot_so");

            entity.HasOne(d => d.Vehicle).WithOne(p => p.Allotment)
                .HasForeignKey<Allotment>(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_allot_vehicle");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("audit_logs")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.ActorUserId, "fk_audit_user");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20)")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .HasColumnName("action");
            entity.Property(e => e.ActorUserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("actor_user_id");
            entity.Property(e => e.Changes)
                .HasColumnType("json")
                .HasColumnName("changes");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Entity)
                .HasMaxLength(50)
                .HasColumnName("entity");
            entity.Property(e => e.EntityId)
                .HasColumnType("bigint(20)")
                .HasColumnName("entity_id");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("fk_audit_user");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("PRIMARY");

            entity
                .ToTable("brands")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.BrandId)
                .HasColumnType("int(11)")
                .HasColumnName("brand_id");
            entity.Property(e => e.Country)
                .HasMaxLength(80)
                .HasColumnName("country");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PRIMARY");

            entity
                .ToTable("customers")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.IdNo)
                .HasMaxLength(50)
                .HasColumnName("id_no");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.DocId).HasName("PRIMARY");

            entity
                .ToTable("documents")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.DocNo, "doc_no").IsUnique();

            entity.HasIndex(e => e.CustomerId, "fk_doc_customer");

            entity.HasIndex(e => e.CreatedBy, "fk_doc_user");

            entity.Property(e => e.DocId)
                .HasColumnType("bigint(20)")
                .HasColumnName("doc_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.DocDate).HasColumnName("doc_date");
            entity.Property(e => e.DocNo)
                .HasMaxLength(50)
                .HasColumnName("doc_no");
            entity.Property(e => e.DocType)
                .HasMaxLength(20)
                .HasColumnName("doc_type");
            entity.Property(e => e.RelatedId)
                .HasColumnType("bigint(20)")
                .HasColumnName("related_id");
            entity.Property(e => e.RelatedTable)
                .HasMaxLength(50)
                .HasColumnName("related_table");
            entity.Property(e => e.StorageUrl)
                .HasColumnType("text")
                .HasColumnName("storage_url");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Documents)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_doc_user");

            entity.HasOne(d => d.Customer).WithMany(p => p.Documents)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("fk_doc_customer");
        });

        modelBuilder.Entity<DocumentLink>(entity =>
        {
            entity.HasKey(e => e.LinkId).HasName("PRIMARY");

            entity
                .ToTable("document_links")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.DocId, "fk_doclink_doc");

            entity.Property(e => e.LinkId)
                .HasColumnType("bigint(20)")
                .HasColumnName("link_id");
            entity.Property(e => e.DocId)
                .HasColumnType("bigint(20)")
                .HasColumnName("doc_id");
            entity.Property(e => e.EntityId)
                .HasColumnType("bigint(20)")
                .HasColumnName("entity_id");
            entity.Property(e => e.EntityTable)
                .HasMaxLength(50)
                .HasColumnName("entity_table");

            entity.HasOne(d => d.Doc).WithMany(p => p.DocumentLinks)
                .HasForeignKey(d => d.DocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_doclink_doc");
        });
        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts");
            entity.HasKey(e => e.GrId).HasName("PRIMARY");

            entity.HasIndex(e => e.PoId, "fk_gr_po");
            entity.HasIndex(e => e.WarehouseId, "fk_gr_wh");
            entity.HasIndex(e => e.CreatedBy, "fk_gr_user");

            entity.Property(e => e.GrId)
                .HasColumnType("bigint(20)")
                .HasColumnName("gr_id");

            entity.Property(e => e.GrNo)
                .HasMaxLength(50)
                .HasColumnName("gr_no");

            entity.Property(e => e.ReceiptDate)
                .HasColumnName("receipt_date");

            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouse_id");

            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            entity.Property(e => e.PoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("po_id");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gr_wh");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_gr_user");

            entity.HasOne(d => d.Po)
                .WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.PoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_gr_po");
        });

        modelBuilder.Entity<GoodsReceiptItem>(entity =>
        {
            entity.HasKey(e => e.GrItemId).HasName("PRIMARY");

            entity
                .ToTable("goods_receipt_items")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.GrId, "fk_gri_gr");

            entity.HasIndex(e => e.VehicleId, "vehicle_id").IsUnique();

            entity.Property(e => e.GrItemId)
                .HasColumnType("bigint(20)")
                .HasColumnName("gr_item_id");
            entity.Property(e => e.GrId)
                .HasColumnType("bigint(20)")
                .HasColumnName("gr_id");
            entity.Property(e => e.LandedCost)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("landed_cost");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Gr).WithMany(p => p.GoodsReceiptItems)
                .HasForeignKey(d => d.GrId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gri_gr");

            entity.HasOne(d => d.Vehicle).WithOne(p => p.GoodsReceiptItem)
                .HasForeignKey<GoodsReceiptItem>(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gri_vehicle");
        });

        modelBuilder.Entity<InventoryMove>(entity =>
        {
            entity.HasKey(e => e.MoveId).HasName("PRIMARY");

            entity
                .ToTable("inventory_moves")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.FromWarehouseId, "fk_inventory_moves_from_wh");

            entity.HasIndex(e => e.ToWarehouseId, "fk_inventory_moves_to_wh");

            entity.HasIndex(e => e.MovedBy, "fk_inventory_moves_user");

            entity.HasIndex(e => e.VehicleId, "fk_inventory_moves_vehicle");

            entity.Property(e => e.MoveId)
                .HasColumnType("bigint(20)")
                .HasColumnName("move_id");
            entity.Property(e => e.FromWarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("from_warehouse_id");
            entity.Property(e => e.MovedAt)
                .HasColumnType("datetime")
                .HasColumnName("moved_at");
            entity.Property(e => e.MovedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("moved_by");
            entity.Property(e => e.Reason)
                .HasMaxLength(20)
                .HasColumnName("reason");
            entity.Property(e => e.ToWarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("to_warehouse_id");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.FromWarehouse).WithMany(p => p.InventoryMoveFromWarehouses)
                .HasForeignKey(d => d.FromWarehouseId)
                .HasConstraintName("fk_inventory_moves_from_wh");

            entity.HasOne(d => d.MovedByNavigation).WithMany(p => p.InventoryMoves)
                .HasForeignKey(d => d.MovedBy)
                .HasConstraintName("fk_inventory_moves_user");

            entity.HasOne(d => d.ToWarehouse).WithMany(p => p.InventoryMoveToWarehouses)
                .HasForeignKey(d => d.ToWarehouseId)
                .HasConstraintName("fk_inventory_moves_to_wh");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.InventoryMoves)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inventory_moves_vehicle");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PRIMARY");

            entity
                .ToTable("invoices")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.CustomerId, "fk_inv_customer");

            entity.HasIndex(e => e.SoId, "fk_inv_so");

            entity.HasIndex(e => e.CreatedBy, "fk_inv_user");

            entity.HasIndex(e => e.InvoiceNo, "invoice_no").IsUnique();

            entity.Property(e => e.InvoiceId)
                .HasColumnType("bigint(20)")
                .HasColumnName("invoice_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.Discount)
                .HasPrecision(14, 2)
                .HasColumnName("discount");
            entity.Property(e => e.GrandTotal)
                .HasPrecision(14, 2)
                .HasColumnName("grand_total");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.InvoiceNo)
                .HasMaxLength(50)
                .HasColumnName("invoice_no");
            entity.Property(e => e.SoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("so_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasPrecision(14, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.Tax)
                .HasPrecision(14, 2)
                .HasColumnName("tax");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_inv_user");

            entity.HasOne(d => d.Customer).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inv_customer");

            entity.HasOne(d => d.So).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.SoId)
                .HasConstraintName("fk_inv_so");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.InvItemId).HasName("PRIMARY");

            entity
                .ToTable("invoice_items")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.InvoiceId, "fk_invi_inv");

            entity.HasIndex(e => e.VehicleId, "fk_invi_vehicle");

            entity.Property(e => e.InvItemId)
                .HasColumnType("bigint(20)")
                .HasColumnName("inv_item_id");
            entity.Property(e => e.Discount)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("discount");
            entity.Property(e => e.InvoiceId)
                .HasColumnType("bigint(20)")
                .HasColumnName("invoice_id");
            entity.Property(e => e.LineTotal)
                .HasPrecision(14, 2)
                .HasColumnName("line_total");
            entity.Property(e => e.Tax)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("tax");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(14, 2)
                .HasColumnName("unit_price");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_invi_inv");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_invi_vehicle");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PRIMARY");

            entity
                .ToTable("payments")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.CustomerId, "fk_pay_customer");

            entity.HasIndex(e => e.ReceiptNo, "receipt_no").IsUnique();

            entity.Property(e => e.PaymentId)
                .HasColumnType("bigint(20)")
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(14, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .HasColumnName("method");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.ReceiptNo)
                .HasMaxLength(50)
                .HasColumnName("receipt_no");

            entity.HasOne(d => d.Customer).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pay_customer");
        });

        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocId).HasName("PRIMARY");

            entity
                .ToTable("payment_allocations")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.InvoiceId, "fk_alloc_invoice");

            entity.HasIndex(e => e.PaymentId, "fk_alloc_pay");

            entity.Property(e => e.AllocId)
                .HasColumnType("bigint(20)")
                .HasColumnName("alloc_id");
            entity.Property(e => e.AmountApplied)
                .HasPrecision(14, 2)
                .HasColumnName("amount_applied");
            entity.Property(e => e.InvoiceId)
                .HasColumnType("bigint(20)")
                .HasColumnName("invoice_id");
            entity.Property(e => e.PaymentId)
                .HasColumnType("bigint(20)")
                .HasColumnName("payment_id");

            entity.HasOne(d => d.Invoice).WithMany(p => p.PaymentAllocations)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_alloc_invoice");

            entity.HasOne(d => d.Payment).WithMany(p => p.PaymentAllocations)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_alloc_pay");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PoId).HasName("PRIMARY");

            entity
                .ToTable("purchase_orders")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.SupplierId, "fk_po_supplier");

            entity.HasIndex(e => e.CreatedBy, "fk_po_user");

            entity.HasIndex(e => e.PoNo, "po_no").IsUnique();

            entity.Property(e => e.PoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("po_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.PoNo)
                .HasMaxLength(50)
                .HasColumnName("po_no");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.SupplierId)
                .HasColumnType("int(11)")
                .HasColumnName("supplier_id");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(14, 2)
                .HasColumnName("total_amount");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_po_user");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_po_supplier");
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.PoItemId).HasName("PRIMARY");

            entity
                .ToTable("purchase_order_items")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.ModelId, "fk_poi_model");

            entity.HasIndex(e => e.PoId, "fk_poi_po");

            entity.Property(e => e.PoItemId)
                .HasColumnType("bigint(20)")
                .HasColumnName("po_item_id");
            entity.Property(e => e.LineTotal)
                .HasPrecision(14, 2)
                .HasColumnName("line_total");
            entity.Property(e => e.ModelId)
                .HasColumnType("int(11)")
                .HasColumnName("model_id");
            entity.Property(e => e.PoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("po_id");
            entity.Property(e => e.Qty)
                .HasColumnType("int(11)")
                .HasColumnName("qty");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(14, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Model).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_poi_model");

            entity.HasOne(d => d.Po).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.PoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_poi_po");
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.HasKey(e => e.QuoteId).HasName("PRIMARY");

            entity
                .ToTable("quotations")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.CustomerId, "fk_quote_customer");

            entity.HasIndex(e => e.CreatedBy, "fk_quote_user");

            entity.HasIndex(e => e.QuoteNo, "quote_no").IsUnique();

            entity.Property(e => e.QuoteId)
                .HasColumnType("bigint(20)")
                .HasColumnName("quote_id");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.Discount)
                .HasPrecision(14, 2)
                .HasColumnName("discount");
            entity.Property(e => e.GrandTotal)
                .HasPrecision(14, 2)
                .HasColumnName("grand_total");
            entity.Property(e => e.QuoteDate).HasColumnName("quote_date");
            entity.Property(e => e.QuoteNo)
                .HasMaxLength(50)
                .HasColumnName("quote_no");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasPrecision(14, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.Tax)
                .HasPrecision(14, 2)
                .HasColumnName("tax");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_quote_user");

            entity.HasOne(d => d.Customer).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_quote_customer");
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.HasKey(e => e.QuoteItemId).HasName("PRIMARY");

            entity
                .ToTable("quotation_items")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.ModelId, "fk_qi_model");

            entity.HasIndex(e => e.QuoteId, "fk_qi_quote");

            entity.Property(e => e.QuoteItemId)
                .HasColumnType("bigint(20)")
                .HasColumnName("quote_item_id");
            entity.Property(e => e.LineTotal)
                .HasPrecision(14, 2)
                .HasColumnName("line_total");
            entity.Property(e => e.ModelId)
                .HasColumnType("int(11)")
                .HasColumnName("model_id");
            entity.Property(e => e.Qty)
                .HasColumnType("int(11)")
                .HasColumnName("qty");
            entity.Property(e => e.QuoteId)
                .HasColumnType("bigint(20)")
                .HasColumnName("quote_id");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(14, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Model).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_qi_model");

            entity.HasOne(d => d.Quote).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.QuoteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_qi_quote");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity
                .ToTable("roles")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.RoleId)
                .HasColumnType("int(11)")
                .HasColumnName("role_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.SoId).HasName("PRIMARY");

            entity
                .ToTable("sales_orders")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.AssignedTo, "fk_so_assigned_to");

            entity.HasIndex(e => e.CreatedBy, "fk_so_created_by");

            entity.HasIndex(e => new { e.CustomerId, e.Status, e.OrderDate }, "ix_so_cust_status_date");

            entity.HasIndex(e => e.SoNo, "so_no").IsUnique();

            entity.Property(e => e.SoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("so_id");
            entity.Property(e => e.AssignedTo)
                .HasColumnType("bigint(20)")
                .HasColumnName("assigned_to");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.Discount)
                .HasPrecision(14, 2)
                .HasColumnName("discount");
            entity.Property(e => e.GrandTotal)
                .HasPrecision(14, 2)
                .HasColumnName("grand_total");
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.SoNo)
                .HasMaxLength(50)
                .HasColumnName("so_no");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasPrecision(14, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.Tax)
                .HasPrecision(14, 2)
                .HasColumnName("tax");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.SalesOrderAssignedToNavigations)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("fk_so_assigned_to");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SalesOrderCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_so_created_by");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_so_customer");
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.HasKey(e => e.SoItemId).HasName("PRIMARY");

            entity
                .ToTable("sales_order_items")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.SoId, "fk_soi_so");

            entity.HasIndex(e => e.VehicleId, "vehicle_id").IsUnique();

            entity.Property(e => e.SoItemId)
                .HasColumnType("bigint(20)")
                .HasColumnName("so_item_id");
            entity.Property(e => e.Discount)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("discount");
            entity.Property(e => e.LineTotal)
                .HasPrecision(14, 2)
                .HasColumnName("line_total");
            entity.Property(e => e.SellPrice)
                .HasPrecision(14, 2)
                .HasColumnName("sell_price");
            entity.Property(e => e.SoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("so_id");
            entity.Property(e => e.Tax)
                .HasPrecision(14, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("tax");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.So).WithMany(p => p.SalesOrderItems)
                .HasForeignKey(d => d.SoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_soi_so");

            entity.HasOne(d => d.Vehicle).WithOne(p => p.SalesOrderItem)
                .HasForeignKey<SalesOrderItem>(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_soi_vehicle");
        });

        modelBuilder.Entity<ServiceOrder>(entity =>
        {
            entity.HasKey(e => e.SvcId).HasName("PRIMARY");

            entity
                .ToTable("service_orders")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.CreatedBy, "fk_svc_user");

            entity.HasIndex(e => e.VehicleId, "fk_svc_vehicle");

            entity.HasIndex(e => e.SvcNo, "svc_no").IsUnique();

            entity.Property(e => e.SvcId)
                .HasColumnType("bigint(20)")
                .HasColumnName("svc_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("bigint(20)")
                .HasColumnName("created_by");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.SvcNo)
                .HasMaxLength(50)
                .HasColumnName("svc_no");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");
            
            // Thêm mapping cho các property bổ sung nếu có trong entity
            entity.Property(e => e.PoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("po_id")
                .IsRequired(false);
            
            entity.Property(e => e.GrId)
                .HasColumnType("bigint(20)")
                .HasColumnName("gr_id")
                .IsRequired(false);
            
            entity.Property(e => e.ModelId)
                .HasColumnType("int(11)")
                .HasColumnName("model_id");
            
            entity.Property(e => e.QuantityExpected)
                .HasColumnType("int(11)")
                .HasColumnName("quantity_expected");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ServiceOrders)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_svc_user");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.ServiceOrders)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_svc_vehicle");
            
            // Thêm relationship với Model nếu có
            entity.HasOne(d => d.Model)
                .WithMany()
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_svc_model");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PRIMARY");

            entity
                .ToTable("suppliers")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.SupplierId)
                .HasColumnType("int(11)")
                .HasColumnName("supplier_id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity
                .ToTable("users")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.UserId)
                .HasColumnType("bigint(20)")
                .HasColumnName("user_id");
            entity.Property(e => e.Active)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("active");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_roles_role"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_roles_user"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j
                            .ToTable("user_roles")
                            .UseCollation("utf8mb4_general_ci");
                        j.HasIndex(new[] { "RoleId" }, "fk_user_roles_role");
                        j.IndexerProperty<long>("UserId")
                            .HasColumnType("bigint(20)")
                            .HasColumnName("user_id");
                        j.IndexerProperty<int>("RoleId")
                            .HasColumnType("int(11)")
                            .HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PRIMARY");

            entity
                .ToTable("vehicles")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.EngineNo, "engine_no").IsUnique();

            entity.HasIndex(e => e.CurrentWarehouseId, "fk_vehicles_wh");

            entity.HasIndex(e => new { e.ModelId, e.Color }, "ix_vehicles_model_color");

            entity.HasIndex(e => new { e.Status, e.CurrentWarehouseId }, "ix_vehicles_status_wh");

            entity.HasIndex(e => e.Vin, "vin").IsUnique();

            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");
            entity.Property(e => e.AcquiredAt)
                .HasColumnType("datetime")
                .HasColumnName("acquired_at");
            entity.Property(e => e.Color)
                .HasMaxLength(50)
                .HasColumnName("color");
            entity.Property(e => e.CurrentWarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("current_warehouse_id");
            entity.Property(e => e.EngineNo)
                .HasMaxLength(64)
                .HasColumnName("engine_no");
            entity.Property(e => e.ModelId)
                .HasColumnType("int(11)")
                .HasColumnName("model_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Vin)
                .HasMaxLength(64)
                .HasColumnName("vin");
            entity.Property(e => e.Year)
                .HasColumnType("int(11)")
                .HasColumnName("year");

            entity.HasOne(d => d.CurrentWarehouse).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.CurrentWarehouseId)
                .HasConstraintName("fk_vehicles_wh");

            entity.HasOne(d => d.Model).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_vehicles_model");
        });

        modelBuilder.Entity<VehicleImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PRIMARY");

            entity
                .ToTable("vehicle_images")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.VehicleId, "fk_vehicle_images_vehicle");

            entity.Property(e => e.ImageId)
                .HasColumnType("bigint(20)")
                .HasColumnName("image_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Kind)
                .HasMaxLength(20)
                .HasColumnName("kind");
            entity.Property(e => e.SortOrder)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)")
                .HasColumnName("sort_order");
            entity.Property(e => e.Url)
                .HasColumnType("text")
                .HasColumnName("url");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.VehicleImages)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_vehicle_images_vehicle");
        });

        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.HasKey(e => e.ModelId).HasName("PRIMARY");

            entity
                .ToTable("vehicle_models")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.BrandId, "fk_vehicle_models_brand");

            entity.HasIndex(e => e.ModelNo, "model_no").IsUnique();

            entity.Property(e => e.ModelId)
                .HasColumnType("int(11)")
                .HasColumnName("model_id");
            entity.Property(e => e.Active)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("active");
            entity.Property(e => e.BasePrice)
                .HasPrecision(14, 2)
                .HasColumnName("base_price");
            entity.Property(e => e.BrandId)
                .HasColumnType("int(11)")
                .HasColumnName("brand_id");
            entity.Property(e => e.FuelType)
                .HasMaxLength(20)
                .HasColumnName("fuel_type");
            entity.Property(e => e.ModelNo)
                .HasMaxLength(50)
                .HasColumnName("model_no");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.SeatNo)
                .HasColumnType("int(11)")
                .HasColumnName("seat_no");
            entity.Property(e => e.Specs)
                .HasColumnType("json")
                .HasColumnName("specs");
            entity.Property(e => e.Transmission)
                .HasMaxLength(10)
                .HasColumnName("transmission");

            entity.HasOne(d => d.Brand).WithMany(p => p.VehicleModels)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_vehicle_models_brand");
        });

        modelBuilder.Entity<VehicleRegistration>(entity =>
        {
            entity.HasKey(e => e.RegId).HasName("PRIMARY");

            entity
                .ToTable("vehicle_registrations")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.VehicleId, "vehicle_id").IsUnique();

            entity.Property(e => e.RegId)
                .HasColumnType("bigint(20)")
                .HasColumnName("reg_id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.Fields)
                .HasColumnType("json")
                .HasColumnName("fields");
            entity.Property(e => e.OwnerName)
                .HasMaxLength(200)
                .HasColumnName("owner_name");
            entity.Property(e => e.RegDate).HasColumnName("reg_date");
            entity.Property(e => e.RegNo)
                .HasMaxLength(50)
                .HasColumnName("reg_no");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");

            entity.HasOne(d => d.Vehicle).WithOne(p => p.VehicleRegistration)
                .HasForeignKey<VehicleRegistration>(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_reg_vehicle");
        });

        modelBuilder.Entity<Waitlist>(entity =>
        {
            entity.HasKey(e => e.WaitlistId).HasName("PRIMARY");

            entity
                .ToTable("waitlists")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.WaitlistId)
                .HasColumnType("int(11)")
                .HasColumnName("waitlist_id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.HasKey(e => e.EntryId).HasName("PRIMARY");

            entity
                .ToTable("waitlist_entries")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.CustomerId, "fk_wle_customer");

            entity.HasIndex(e => e.ModelId, "fk_wle_model");

            entity.HasIndex(e => e.WaitlistId, "fk_wle_waitlist");

            entity.Property(e => e.EntryId)
                .HasColumnType("bigint(20)")
                .HasColumnName("entry_id");
            entity.Property(e => e.CustomerId)
                .HasColumnType("bigint(20)")
                .HasColumnName("customer_id");
            entity.Property(e => e.ModelId)
                .HasColumnType("int(11)")
                .HasColumnName("model_id");
            entity.Property(e => e.PreferredColor)
                .HasMaxLength(50)
                .HasColumnName("preferred_color");
            entity.Property(e => e.RequestedDate).HasColumnName("requested_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.WaitlistId)
                .HasColumnType("int(11)")
                .HasColumnName("waitlist_id");

            entity.HasOne(d => d.Customer).WithMany(p => p.WaitlistEntries)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wle_customer");

            entity.HasOne(d => d.Model).WithMany(p => p.WaitlistEntries)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wle_model");

            entity.HasOne(d => d.Waitlist).WithMany(p => p.WaitlistEntries)
                .HasForeignKey(d => d.WaitlistId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wle_waitlist");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PRIMARY");

            entity
                .ToTable("warehouses")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.WarehouseId)
                .HasColumnType("int(11)")
                .HasColumnName("warehouse_id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        // ------------------ GOODS RETURN ------------------
        modelBuilder.Entity<GoodsReturn>(entity =>
        {
            entity.HasKey(e => e.GrtId).HasName("PRIMARY");

            entity
                .ToTable("goods_returns")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.PoId, "fk_grt_po");
            entity.HasIndex(e => e.SupplierId, "fk_grt_supplier");

            entity.Property(e => e.GrtId)
                .HasColumnType("bigint(20)")
                .HasColumnName("grt_id");
            entity.Property(e => e.GrtNo)
                .HasMaxLength(50)
                .HasColumnName("grt_no");
            entity.Property(e => e.PoId)
                .HasColumnType("bigint(20)")
                .HasColumnName("po_id");
            entity.Property(e => e.SupplierId)
                .HasColumnType("int(11)")
                .HasColumnName("supplier_id");
            entity.Property(e => e.ReturnDate)
                .HasColumnName("return_date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
        });

        // ------------------ GOODS RETURN ITEM ------------------
        modelBuilder.Entity<GoodsReturnItem>(entity =>
        {
            entity.HasKey(e => e.GrtItemId).HasName("PRIMARY");

            entity
                .ToTable("goods_return_items")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.GrtId, "fk_grti_grt");

            entity.Property(e => e.GrtItemId)
                .HasColumnType("bigint(20)")
                .HasColumnName("grt_item_id");
            entity.Property(e => e.GrtId)
                .HasColumnType("bigint(20)")
                .HasColumnName("grt_id");
            entity.Property(e => e.VehicleId)
                .HasColumnType("bigint(20)")
                .HasColumnName("vehicle_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasColumnName("reason");

            entity.HasOne(d => d.GoodsReturn)
                .WithMany(p => p.GoodsReturnItems)
                .HasForeignKey(d => d.GrtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_grti_grt");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
