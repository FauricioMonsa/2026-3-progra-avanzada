using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Quiniela.Model;

public partial class QuinielaDbContext : DbContext
{
    public QuinielaDbContext(DbContextOptions<QuinielaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Forecast> Forecasts { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<MatchParticipant> MatchParticipants { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Tournament> Tournaments { get; set; }

    public virtual DbSet<TournamentTeam> TournamentTeams { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("user_08", "match_kind", new[] { "competition", "friendly" })
            .HasPostgresEnum("user_08", "match_status", new[] { "scheduled", "live", "finished" })
            .HasPostgresEnum("user_08", "sport_type", new[] { "soccer", "basketball", "baseball", "volleyball", "rugby", "hockey" });

        modelBuilder.Entity<Forecast>(entity =>
        {
            entity.HasKey(e => e.ForecastId).HasName("forecasts_pkey");

            entity.ToTable("forecasts", "user_08");

            entity.HasIndex(e => new { e.MatchId, e.UserId }, "forecasts_match_id_user_id_key").IsUnique();

            entity.HasIndex(e => e.UserId, "ix_forecasts_user");

            entity.Property(e => e.ForecastId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("forecast_id");
            entity.Property(e => e.AwayScore).HasColumnName("away_score");
            entity.Property(e => e.HomeScore).HasColumnName("home_score");
            entity.Property(e => e.MatchId).HasColumnName("match_id");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("clock_timestamp()")
                .HasColumnName("submitted_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Match).WithMany(p => p.Forecasts)
                .HasForeignKey(d => d.MatchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("forecasts_match_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Forecasts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("forecasts_user_id_fkey");
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.MatchId).HasName("matches_pkey");

            entity.ToTable("matches", "user_08");

            entity.HasIndex(e => e.TournamentId, "ix_matches_tournament");

            entity.Property(e => e.MatchId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("match_id");
            entity.Property(e => e.AwayScore).HasColumnName("away_score");
            entity.Property(e => e.AwayTeamId).HasColumnName("away_team_id");
            entity.Property(e => e.HomeScore).HasColumnName("home_score");
            entity.Property(e => e.HomeTeamId).HasColumnName("home_team_id");
            entity.Property(e => e.Kind)
                .HasColumnType("user_08.match_kind")
                .HasColumnName("kind");
            entity.Property(e => e.KickoffAt).HasColumnName("kickoff_at");
            entity.Property(e => e.MatchDay).HasColumnName("match_day");
            entity.Property(e => e.RoundName).HasColumnName("round_name");
            entity.Property(e => e.Sport)
                .HasColumnType("user_08.sport_type")
                .HasColumnName("sport");
            entity.Property(e => e.Status)
                .HasColumnType("user_08.match_status")
                .HasColumnName("status");
            entity.Property(e => e.Stats)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("stats");
            entity.Property(e => e.TimeZone)
                .HasDefaultValueSql("'UTC'::text")
                .HasColumnName("time_zone");
            entity.Property(e => e.TournamentId).HasColumnName("tournament_id");
        });

        modelBuilder.Entity<MatchParticipant>(entity =>
        {
            entity.HasKey(e => new { e.MatchId, e.TeamId }).HasName("match_participants_pkey");

            entity.ToTable("match_participants", "user_08");

            entity.HasIndex(e => new { e.TeamId, e.MatchDay }, "match_participants_team_id_match_day_key").IsUnique();

            entity.Property(e => e.MatchId).HasColumnName("match_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.MatchDay).HasColumnName("match_day");

            entity.HasOne(d => d.Match).WithMany(p => p.MatchParticipants)
                .HasForeignKey(d => d.MatchId)
                .HasConstraintName("match_participants_match_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.MatchParticipants)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("match_participants_team_id_fkey");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("teams_pkey");

            entity.ToTable("teams", "user_08");

            entity.Property(e => e.TeamId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("team_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Sport)
                .HasColumnType("user_08.sport_type")
                .HasColumnName("sport");
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.HasKey(e => e.TournamentId).HasName("tournaments_pkey");

            entity.ToTable("tournaments", "user_08");

            entity.Property(e => e.TournamentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("tournament_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SeasonYear).HasColumnName("season_year");
            entity.Property(e => e.Sport)
                .HasColumnType("user_08.sport_type")
                .HasColumnName("sport");
            entity.Property(e => e.TimeZone)
                .HasDefaultValueSql("'Europe/Madrid'::text")
                .HasColumnName("time_zone");
        });

        modelBuilder.Entity<TournamentTeam>(entity =>
        {
            entity.HasKey(e => new { e.TournamentId, e.TeamId }).HasName("tournament_teams_pkey");

            entity.ToTable("tournament_teams", "user_08");

            entity.Property(e => e.TournamentId).HasColumnName("tournament_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.Sport)
                .HasColumnType("user_08.sport_type")
                .HasColumnName("sport");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users", "user_08");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.UserId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("user_id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Username).HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
