﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.TestModels.GearsOfWarModel;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class GearsOfWarQueryFixtureBase : SharedStoreFixtureBase<GearsOfWarContext>, IQueryFixtureBase
    {
        protected override string StoreName { get; } = "GearsOfWarQueryTest";

        protected GearsOfWarQueryFixtureBase()
        {
            var entitySorters = new Dictionary<Type, Func<dynamic, object>>();

            var entityAsserters = new Dictionary<Type, Action<dynamic, dynamic>>();

            QueryAsserter = new QueryAsserter<GearsOfWarContext>(
                CreateContext,
                new GearsOfWarData(),
                entitySorters,
                entityAsserters);
        }

        public QueryAsserterBase QueryAsserter { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.Entity<City>().HasKey(c => c.Name);

            modelBuilder.Entity<Gear>(
                b =>
                    {
                        b.HasKey(g => new { g.Nickname, g.SquadId });

                        b.HasOne(g => g.CityOfBirth).WithMany(c => c.BornGears).HasForeignKey(g => g.CityOrBirthName).IsRequired();
                        b.HasOne(g => g.Tag).WithOne(t => t.Gear).HasForeignKey<CogTag>(t => new { t.GearNickName, t.GearSquadId });
                        b.HasOne(g => g.AssignedCity).WithMany(c => c.StationedGears).IsRequired(false);
                    });

            modelBuilder.Entity<Officer>().HasMany(o => o.Reports).WithOne().HasForeignKey(o => new { o.LeaderNickname, o.LeaderSquadId });

            modelBuilder.Entity<Squad>(
                b =>
                    {
                        b.HasKey(s => s.Id);
                        b.Property(s => s.Id).ValueGeneratedNever();
                        b.HasMany(s => s.Members).WithOne(g => g.Squad).HasForeignKey(g => g.SquadId);
                    });

            modelBuilder.Entity<Weapon>(
                b =>
                    {
                        b.Property(w => w.Id).ValueGeneratedNever();
                        b.HasOne(w => w.SynergyWith).WithOne().HasForeignKey<Weapon>(w => w.SynergyWithId);
                        b.HasOne(w => w.Owner).WithMany(g => g.Weapons).HasForeignKey(w => w.OwnerFullName).HasPrincipalKey(g => g.FullName);
                    });

            modelBuilder.Entity<Mission>().Property(m => m.Id).ValueGeneratedNever();

            modelBuilder.Entity<SquadMission>(
                b =>
                    {
                        b.HasKey(sm => new { sm.SquadId, sm.MissionId });
                        b.HasOne(sm => sm.Mission).WithMany(m => m.ParticipatingSquads).HasForeignKey(sm => sm.MissionId);
                        b.HasOne(sm => sm.Squad).WithMany(s => s.Missions).HasForeignKey(sm => sm.SquadId);
                    });

            modelBuilder.Entity<Faction>().HasKey(f => f.Id);
            modelBuilder.Entity<Faction>().Property(f => f.Id).ValueGeneratedNever();

            modelBuilder.Entity<LocustHorde>().HasBaseType<Faction>();
            modelBuilder.Entity<LocustHorde>().HasMany(h => h.Leaders).WithOne();

            modelBuilder.Entity<LocustHorde>().HasOne(h => h.Commander).WithOne(c => c.CommandingFaction);

            modelBuilder.Entity<LocustLeader>().HasKey(l => l.Name);
            modelBuilder.Entity<LocustCommander>().HasBaseType<LocustLeader>();
            modelBuilder.Entity<LocustCommander>().HasOne(c => c.DefeatedBy).WithOne().HasForeignKey<LocustCommander>(c => new { c.DefeatedByNickname, c.DefeatedBySquadId });

            modelBuilder.Entity<LocustHighCommand>().HasKey(l => l.Id);
            modelBuilder.Entity<LocustHighCommand>().Property(l => l.Id).ValueGeneratedNever();
        }

        protected override void Seed(GearsOfWarContext context) => GearsOfWarContext.Seed(context);

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder).ConfigureWarnings(
                c => c
                    .Log(CoreEventId.IncludeIgnoredWarning));

        public override GearsOfWarContext CreateContext()
        {
            var context = base.CreateContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return context;
        }
    }
}
