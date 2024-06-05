﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Scalection.Data.EF;

#nullable disable

namespace Scalection.Data.EF.Migrations
{
    [DbContext(typeof(ScalectionContext))]
    partial class ScalectionContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Scalection.Data.EF.Models.Candidate", b =>
                {
                    b.Property<Guid>("CandidateId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("PartyId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("CandidateId");

                    b.HasIndex("PartyId");

                    b.ToTable("Candidates");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Election", b =>
                {
                    b.Property<Guid>("ElectionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ElectionId");

                    b.ToTable("Elections");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.ElectionDistrict", b =>
                {
                    b.Property<long>("ElectionDistrictId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("ElectionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("ElectionDistrictId");

                    b.HasIndex("ElectionId");

                    b.ToTable("ElectionDistricts");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Party", b =>
                {
                    b.Property<Guid>("PartyId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ElectionId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("PartyId");

                    b.HasIndex("ElectionId");

                    b.ToTable("Parties");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Vote", b =>
                {
                    b.Property<Guid>("VoteId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("CandidateId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<long>("ElectionDistrictId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("PartyId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("VoteId");

                    b.HasIndex("CandidateId");

                    b.HasIndex("ElectionDistrictId");

                    b.HasIndex("PartyId");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Voter", b =>
                {
                    b.Property<long>("VoterId")
                        .HasColumnType("bigint");

                    b.Property<long>("ElectionDistrictId")
                        .HasColumnType("bigint");

                    b.Property<bool>("Voted")
                        .HasColumnType("bit");

                    b.HasKey("VoterId");

                    b.HasIndex("ElectionDistrictId");

                    b.ToTable("Voters");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Candidate", b =>
                {
                    b.HasOne("Scalection.Data.EF.Models.Party", null)
                        .WithMany("Candidates")
                        .HasForeignKey("PartyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.ElectionDistrict", b =>
                {
                    b.HasOne("Scalection.Data.EF.Models.Election", null)
                        .WithMany()
                        .HasForeignKey("ElectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Party", b =>
                {
                    b.HasOne("Scalection.Data.EF.Models.Election", "Election")
                        .WithMany()
                        .HasForeignKey("ElectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Election");
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Vote", b =>
                {
                    b.HasOne("Scalection.Data.EF.Models.Candidate", null)
                        .WithMany()
                        .HasForeignKey("CandidateId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Scalection.Data.EF.Models.ElectionDistrict", null)
                        .WithMany()
                        .HasForeignKey("ElectionDistrictId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Scalection.Data.EF.Models.Party", null)
                        .WithMany()
                        .HasForeignKey("PartyId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Voter", b =>
                {
                    b.HasOne("Scalection.Data.EF.Models.ElectionDistrict", null)
                        .WithMany()
                        .HasForeignKey("ElectionDistrictId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Scalection.Data.EF.Models.Party", b =>
                {
                    b.Navigation("Candidates");
                });
#pragma warning restore 612, 618
        }
    }
}
