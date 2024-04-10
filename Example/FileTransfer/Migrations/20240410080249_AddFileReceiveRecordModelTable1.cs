using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileTransfer.Migrations
{
    /// <inheritdoc />
    public partial class AddFileReceiveRecordModelTable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileReceiveRecordModels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TempFileSaveLocation = table.Column<string>(type: "TEXT", nullable: true),
                    FileSaveLocation = table.Column<string>(type: "TEXT", nullable: true),
                    FileSendId = table.Column<string>(type: "TEXT", nullable: false),
                    LastRemoteEndpoint = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    TotalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FinishTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileReceiveRecordModels", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileReceiveRecordModels");
        }
    }
}
