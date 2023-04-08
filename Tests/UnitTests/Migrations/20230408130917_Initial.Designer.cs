﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UnitTests;

#nullable disable

namespace UnitTests.Migrations
{
    [DbContext(typeof(TestModelDbContext))]
    [Migration("20230408130917_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("EntityAEntityB", b =>
                {
                    b.Property<int>("ManyToManyEntitiesAId")
                        .HasColumnType("int");

                    b.Property<int>("ManyToManyEntitiesBId")
                        .HasColumnType("int");

                    b.HasKey("ManyToManyEntitiesAId", "ManyToManyEntitiesBId");

                    b.HasIndex("ManyToManyEntitiesBId");

                    b.ToTable("EntityAEntityB");
                });

            modelBuilder.Entity("TestModel.AggregateA", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("RequiredText")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("AggregateAs", (string)null);
                });

            modelBuilder.Entity("TestModel.AggregateB", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AggregateAId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("OptionalDateTimeOffset")
                        .HasColumnType("datetimeoffset");

                    b.Property<double?>("OptionalNumber")
                        .HasColumnType("float");

                    b.Property<string>("OptionalText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("RequiredDateTimeOffset")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("RequiredText")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("AggregateAId");

                    b.ToTable("AggregateBs", (string)null);
                });

            modelBuilder.Entity("TestModel.CompositeEntity", b =>
                {
                    b.Property<int>("AggregateAId")
                        .HasColumnType("int");

                    b.Property<int>("SomeId")
                        .HasColumnType("int");

                    b.Property<string>("RequiredText")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("AggregateAId", "SomeId");

                    b.ToTable("CompositeEntities", (string)null);
                });

            modelBuilder.Entity("TestModel.EntityA", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AggregateBId")
                        .HasColumnType("int");

                    b.Property<string>("RequiredText")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.HasIndex("AggregateBId");

                    b.ToTable("EntityAs", (string)null);
                });

            modelBuilder.Entity("TestModel.EntityB", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("RequiredText")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("Id");

                    b.ToTable("EntityBs", (string)null);
                });

            modelBuilder.Entity("TestModel.EntityC", b =>
                {
                    b.Property<int>("AggregateBId")
                        .HasColumnType("int");

                    b.Property<string>("RequiredText")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("AggregateBId");

                    b.ToTable("EntityCs", (string)null);
                });

            modelBuilder.Entity("EntityAEntityB", b =>
                {
                    b.HasOne("TestModel.EntityA", null)
                        .WithMany()
                        .HasForeignKey("ManyToManyEntitiesAId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TestModel.EntityB", null)
                        .WithMany()
                        .HasForeignKey("ManyToManyEntitiesBId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TestModel.AggregateB", b =>
                {
                    b.HasOne("TestModel.AggregateA", "RelatedAggregate")
                        .WithMany("RelatedAggregates")
                        .HasForeignKey("AggregateAId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("RelatedAggregate");
                });

            modelBuilder.Entity("TestModel.CompositeEntity", b =>
                {
                    b.HasOne("TestModel.AggregateA", "RelatedAggregate")
                        .WithMany("CompositeEntities")
                        .HasForeignKey("AggregateAId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("RelatedAggregate");
                });

            modelBuilder.Entity("TestModel.EntityA", b =>
                {
                    b.HasOne("TestModel.AggregateB", null)
                        .WithMany("ManySubEntities")
                        .HasForeignKey("AggregateBId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.OwnsOne("TestModel.ValueObjectA", "OptionalOwnedObject", b1 =>
                        {
                            b1.Property<int>("EntityAId")
                                .HasColumnType("int");

                            b1.Property<bool?>("OptionalBit")
                                .HasColumnType("bit");

                            b1.Property<string>("RequiredText")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("nvarchar(50)");

                            b1.HasKey("EntityAId");

                            b1.ToTable("EntityAs");

                            b1.WithOwner()
                                .HasForeignKey("EntityAId");
                        });

                    b.OwnsMany("TestModel.ValueObjectB", "ManyOwnedObjects", b1 =>
                        {
                            b1.Property<int>("EntityAId")
                                .HasColumnType("int");

                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("Id"));

                            b1.Property<int?>("OptionalInt")
                                .HasColumnType("int");

                            b1.Property<string>("RequiredText")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("nvarchar(50)");

                            b1.HasKey("EntityAId", "Id");

                            b1.ToTable("ValueObjectB");

                            b1.WithOwner()
                                .HasForeignKey("EntityAId");
                        });

                    b.Navigation("ManyOwnedObjects");

                    b.Navigation("OptionalOwnedObject");
                });

            modelBuilder.Entity("TestModel.EntityC", b =>
                {
                    b.HasOne("TestModel.AggregateB", null)
                        .WithOne("OneToOneSubEntity")
                        .HasForeignKey("TestModel.EntityC", "AggregateBId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("TestModel.ValueObjectA", "RequiredOwnedObject", b1 =>
                        {
                            b1.Property<int>("EntityCAggregateBId")
                                .HasColumnType("int");

                            b1.Property<bool?>("OptionalBit")
                                .HasColumnType("bit");

                            b1.Property<string>("RequiredText")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("nvarchar(50)");

                            b1.HasKey("EntityCAggregateBId");

                            b1.ToTable("EntityCs");

                            b1.WithOwner()
                                .HasForeignKey("EntityCAggregateBId");
                        });

                    b.Navigation("RequiredOwnedObject")
                        .IsRequired();
                });

            modelBuilder.Entity("TestModel.AggregateA", b =>
                {
                    b.Navigation("CompositeEntities");

                    b.Navigation("RelatedAggregates");
                });

            modelBuilder.Entity("TestModel.AggregateB", b =>
                {
                    b.Navigation("ManySubEntities");

                    b.Navigation("OneToOneSubEntity");
                });
#pragma warning restore 612, 618
        }
    }
}
