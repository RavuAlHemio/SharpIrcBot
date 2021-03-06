using System.Linq;
using Microsoft.EntityFrameworkCore;
using SharpIrcBot.Util;

namespace SharpIrcBot.Plugins.DasIstNenFehler.ORM
{
    public class GermanWordsContext : DbContext
    {
        protected DbSet<Adjective> RealAdjectives { get; set; }
        protected DbSet<Noun> RealNouns { get; set; }
        protected DbSet<Word> RealWords { get; set; }

        public IQueryable<Adjective> Adjectives => RealAdjectives.AsNoTracking();
        public IQueryable<Noun> Nouns => RealNouns.AsNoTracking();
        public IQueryable<Word> Words => RealWords.AsNoTracking();

        public GermanWordsContext(DbContextOptions<GermanWordsContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.IfNpgsql(Database, b =>
            {
                b.HasSequence<long>("seq__words__word_id", schema: "german_words")
                    .StartsAt(1);
                b.HasSequence<long>("seq__nouns__noun_id", schema: "german_words")
                    .StartsAt(1);
                b.HasSequence<long>("seq__adjectives__adj_id", schema: "german_words")
                    .StartsAt(1);
            });

            builder.Entity<Adjective>(entBuilder =>
            {
                entBuilder.ToTable("adjectives", schema: "german_words");
                entBuilder.HasKey(a => a.ID);
                entBuilder.HasIndex(a => a.WordID)
                    .IfNpgsql(Database, npa =>
                        npa.HasName("idx__adjectives__word")
                    );
                entBuilder.HasIndex(a => new {a.WordID, a.BaseWordID, a.Case, a.Number, a.Gender, a.Comparison})
                    .IsUnique()
                    .IfNpgsql(Database, npa =>
                        npa.HasName("uq__adjectives__all")
                    );

                entBuilder.Property(a => a.ID)
                    .IsRequired()
                    .HasColumnName("adj_id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npa =>
                        npa.HasDefaultValueSql("nextval('german_words.seq__adjectives__adj_id')")
                    );

                entBuilder.Property(a => a.WordID)
                    .IsRequired()
                    .HasColumnName("word");

                entBuilder.Property(a => a.BaseWordID)
                    .IsRequired()
                    .HasColumnName("base_word");

                entBuilder.Property(a => a.Case)
                    .IsRequired()
                    .HasColumnName("gr_case");

                entBuilder.Property(a => a.Number)
                    .IsRequired()
                    .HasColumnName("gr_number");

                entBuilder.Property(a => a.Gender)
                    .IsRequired()
                    .HasColumnName("gr_gender");

                entBuilder.Property(a => a.Comparison)
                    .IsRequired()
                    .HasColumnName("compar");

                entBuilder.HasOne(a => a.BaseWord)
                    .WithMany()
                    .HasForeignKey(a => a.BaseWordID);

                entBuilder.HasOne(a => a.Word)
                    .WithMany()
                    .HasForeignKey(a => a.WordID);
            });

            builder.Entity<Noun>(entBuilder =>
            {
                entBuilder.ToTable("nouns", schema: "german_words");
                entBuilder.HasKey(n => n.ID);
                entBuilder.HasIndex(n => n.WordID)
                    .IfNpgsql(Database, npn =>
                        npn.HasName("idx__nouns__word")
                    );
                entBuilder.HasIndex(n => new {n.WordID, n.BaseWordID, n.Case, n.Number, n.Gender})
                    .IsUnique()
                    .IfNpgsql(Database, npn =>
                        npn.HasName("uq__nouns__all")
                    );

                entBuilder.Property(n => n.ID)
                    .IsRequired()
                    .HasColumnName("noun_id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npn =>
                        npn.HasDefaultValueSql("nextval('german_words.seq__nouns__noun_id')")
                    );

                entBuilder.Property(n => n.WordID)
                    .IsRequired()
                    .HasColumnName("word");

                entBuilder.Property(n => n.BaseWordID)
                    .IsRequired()
                    .HasColumnName("base_word");

                entBuilder.Property(n => n.Case)
                    .IsRequired()
                    .HasColumnName("gr_case");

                entBuilder.Property(n => n.Number)
                    .IsRequired()
                    .HasColumnName("gr_number");

                entBuilder.Property(n => n.Gender)
                    .IsRequired()
                    .HasColumnName("gr_gender");

                entBuilder.HasOne(n => n.BaseWord)
                    .WithMany()
                    .HasForeignKey(n => n.BaseWordID);

                entBuilder.HasOne(n => n.Word)
                    .WithMany()
                    .HasForeignKey(n => n.WordID);
            });

            builder.Entity<Word>(entBuilder =>
            {
                entBuilder.ToTable("words", schema: "german_words");
                entBuilder.HasKey(w => w.ID);
                entBuilder.HasIndex(w => w.WordString)
                    .IsUnique()
                    .IfNpgsql(Database, npn =>
                        npn.HasName("uq__words__word")
                    );
                /*
                entBuilder.HasIndex(w => w.WordString.ToLowerInvariant())
                    .ForNpgsqlHasName("idx__words__word_lower");
                */

                entBuilder.Property(n => n.ID)
                    .IsRequired()
                    .HasColumnName("word_id")
                    .ValueGeneratedOnAdd()
                    .IfNpgsql(Database, npn =>
                        npn.HasDefaultValueSql("nextval('german_words.seq__words__word_id')")
                    );

                entBuilder.Property(n => n.WordString)
                    .IsRequired()
                    .HasColumnName("word")
                    .HasMaxLength(255);
            });
        }
    }
}
