using Microsoft.EntityFrameworkCore;
using YggdrasilWebApp.Models.App;
using YggdrasilWebApp.Models.Yggdrasil;

namespace YggdrasilWebApp.Contexts;

public sealed class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<UserEntity> Users => Set<UserEntity>();
	public DbSet<MinecraftProfileEntity> Profiles => Set<MinecraftProfileEntity>();
	public DbSet<AccessTokenEntity> AccessTokens => Set<AccessTokenEntity>();
	public DbSet<ServerJoinEntity> ServerJoins => Set<ServerJoinEntity>();
	public DbSet<GameModEntity> GameMods => Set<GameModEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<UserEntity>(e =>
		{
			e.ToTable("users");
			e.HasKey(x => x.Id);

			e.Property(x => x.Username).IsRequired().HasMaxLength(128);
			e.Property(x => x.Email).HasMaxLength(256);
			e.Property(x => x.PasswordHash).IsRequired();

			e.HasIndex(x => x.Username).IsUnique();
			e.HasIndex(x => x.Email);

			e.HasMany(x => x.Profiles)
				.WithOne(p => p.User)
				.HasForeignKey(p => p.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<MinecraftProfileEntity>(e =>
		{
			e.ToTable("minecraft_profiles");
			e.HasKey(x => x.Id);

			e.Property(x => x.Name).IsRequired().HasMaxLength(16);
			e.Property(x => x.TexturesValue).HasColumnType("text");
			e.Property(x => x.TexturesSignature).HasColumnType("text");

			e.HasIndex(x => x.Name).IsUnique();
			e.HasIndex(x => x.UserId);
		});

		modelBuilder.Entity<AccessTokenEntity>(e =>
		{
			e.ToTable("access_tokens");
			e.HasKey(x => x.Id);

			e.Property(x => x.Token).IsRequired().HasMaxLength(128);
			e.Property(x => x.ClientToken).HasMaxLength(128);

			e.HasIndex(x => x.Token).IsUnique();
			e.HasIndex(x => x.UserId);

			e.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ServerJoinEntity>(e =>
		{
			e.ToTable("server_joins");
			e.HasKey(x => x.Id);

			e.Property(x => x.ServerId).IsRequired().HasMaxLength(128);
			e.Property(x => x.ClientIp).HasMaxLength(64);

			e.HasIndex(x => new { x.ServerId, x.ProfileId });
			e.HasIndex(x => x.JoinedAtUtc);

			e.HasOne(x => x.Profile)
				.WithMany()
				.HasForeignKey(x => x.ProfileId)
				.OnDelete(DeleteBehavior.Cascade);
		});
	}
}
