using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnitTests.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AggregateAs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateAs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityBs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityBs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AggregateBs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AggregateAId = table.Column<int>(type: "int", nullable: true),
                    OptionalText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionalNumber = table.Column<double>(type: "float", nullable: true),
                    OptionalDateTimeOffset = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredDateTimeOffset = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateBs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateBs_AggregateAs_AggregateAId",
                        column: x => x.AggregateAId,
                        principalTable: "AggregateAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompositeEntities",
                columns: table => new
                {
                    AggregateAId = table.Column<int>(type: "int", nullable: false),
                    SomeId = table.Column<int>(type: "int", nullable: false),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompositeEntities", x => new { x.AggregateAId, x.SomeId });
                    table.ForeignKey(
                        name: "FK_CompositeEntities_AggregateAs_AggregateAId",
                        column: x => x.AggregateAId,
                        principalTable: "AggregateAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EntityAs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OptionalOwnedObject_RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OptionalOwnedObject_OptionalBit = table.Column<bool>(type: "bit", nullable: true),
                    AggregateBId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityAs_AggregateBs_AggregateBId",
                        column: x => x.AggregateBId,
                        principalTable: "AggregateBs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityCs",
                columns: table => new
                {
                    AggregateBId = table.Column<int>(type: "int", nullable: false),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredOwnedObject_RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredOwnedObject_OptionalBit = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityCs", x => x.AggregateBId);
                    table.ForeignKey(
                        name: "FK_EntityCs_AggregateBs_AggregateBId",
                        column: x => x.AggregateBId,
                        principalTable: "AggregateBs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntityAEntityB",
                columns: table => new
                {
                    ManyToManyEntitiesAId = table.Column<int>(type: "int", nullable: false),
                    ManyToManyEntitiesBId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAEntityB", x => new { x.ManyToManyEntitiesAId, x.ManyToManyEntitiesBId });
                    table.ForeignKey(
                        name: "FK_EntityAEntityB_EntityAs_ManyToManyEntitiesAId",
                        column: x => x.ManyToManyEntitiesAId,
                        principalTable: "EntityAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityAEntityB_EntityBs_ManyToManyEntitiesBId",
                        column: x => x.ManyToManyEntitiesBId,
                        principalTable: "EntityBs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValueObjectB",
                columns: table => new
                {
                    EntityAId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiredText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OptionalInt = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueObjectB", x => new { x.EntityAId, x.Id });
                    table.ForeignKey(
                        name: "FK_ValueObjectB_EntityAs_EntityAId",
                        column: x => x.EntityAId,
                        principalTable: "EntityAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateBs_AggregateAId",
                table: "AggregateBs",
                column: "AggregateAId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAEntityB_ManyToManyEntitiesBId",
                table: "EntityAEntityB",
                column: "ManyToManyEntitiesBId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAs_AggregateBId",
                table: "EntityAs",
                column: "AggregateBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompositeEntities");

            migrationBuilder.DropTable(
                name: "EntityAEntityB");

            migrationBuilder.DropTable(
                name: "EntityCs");

            migrationBuilder.DropTable(
                name: "ValueObjectB");

            migrationBuilder.DropTable(
                name: "EntityBs");

            migrationBuilder.DropTable(
                name: "EntityAs");

            migrationBuilder.DropTable(
                name: "AggregateBs");

            migrationBuilder.DropTable(
                name: "AggregateAs");
        }
    }
}
