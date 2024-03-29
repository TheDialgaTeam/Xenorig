﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xenopool.Server.Database;

#nullable disable

namespace Xenopool.Server.Migrations
{
    [DbContext(typeof(SqliteDatabaseContext))]
    [Migration("20230517133250_1.0.2")]
    partial class _102
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("Xenopool.Server.Database.Tables.PoolAccount", b =>
                {
                    b.Property<string>("WalletAddress")
                        .HasColumnType("TEXT");

                    b.Property<string>("BanReason")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsBanned")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MinimumPayoutAmount")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("WalletAmount")
                        .HasColumnType("INTEGER");

                    b.HasKey("WalletAddress");

                    b.ToTable("PoolAccounts");
                });

            modelBuilder.Entity("Xenopool.Server.Database.Tables.PoolShare", b =>
                {
                    b.Property<string>("WalletAddress")
                        .HasColumnType("TEXT");

                    b.Property<long>("Height")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PoolAccountWalletAddress")
                        .HasColumnType("TEXT");

                    b.Property<long>("SharePoints")
                        .HasColumnType("INTEGER");

                    b.Property<string>("WorkerId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("WalletAddress");

                    b.HasIndex("PoolAccountWalletAddress");

                    b.ToTable("PoolShare");
                });

            modelBuilder.Entity("Xenopool.Server.Database.Tables.PoolShare", b =>
                {
                    b.HasOne("Xenopool.Server.Database.Tables.PoolAccount", null)
                        .WithMany("PoolShares")
                        .HasForeignKey("PoolAccountWalletAddress");
                });

            modelBuilder.Entity("Xenopool.Server.Database.Tables.PoolAccount", b =>
                {
                    b.Navigation("PoolShares");
                });
#pragma warning restore 612, 618
        }
    }
}
