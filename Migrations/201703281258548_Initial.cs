namespace Spindel.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Pages",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Url = c.String(nullable: false),
                    LastCrawl = c.DateTime(),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.Relationships",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Child_Id = c.Int(nullable: false),
                    Parent_Id = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Pages", t => t.Child_Id, cascadeDelete: false)
                .ForeignKey("dbo.Pages", t => t.Parent_Id, cascadeDelete: false)
                .Index(t => t.Child_Id)
                .Index(t => t.Parent_Id);

        }

        public override void Down()
        {
            DropForeignKey("dbo.Relationships", "Parent_Id", "dbo.Pages");
            DropForeignKey("dbo.Relationships", "Child_Id", "dbo.Pages");
            DropIndex("dbo.Relationships", new[] { "Parent_Id" });
            DropIndex("dbo.Relationships", new[] { "Child_Id" });
            DropTable("dbo.Relationships");
            DropTable("dbo.Pages");
        }
    }
}
