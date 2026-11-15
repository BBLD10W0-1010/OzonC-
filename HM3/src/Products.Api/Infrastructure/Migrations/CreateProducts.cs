using FluentMigrator;

namespace Products.Api.Infrastructure.Migrations;

[Migration(1)]
public sealed class CreateProducts : Migration
{
    public override void Up()
    {
        Create.Table("products")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("price").AsCustom("numeric").NotNullable()
            .WithColumn("weight").AsDouble().NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
            .WithColumn("warehouse_number").AsInt32().NotNullable();

        Create.Index("ix_products_created_at").OnTable("products")
            .OnColumn("created_at").Ascending().OnColumn("id").Ascending();
        Create.Index("ix_products_type_created_at").OnTable("products")
            .OnColumn("type").Ascending().OnColumn("created_at").Ascending().OnColumn("id").Ascending();
        Create.Index("ix_products_warehouse_created_at").OnTable("products")
            .OnColumn("warehouse_number").Ascending().OnColumn("created_at").Ascending().OnColumn("id").Ascending();
    }

    public override void Down() => Delete.Table("products");
}
