using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LazyLoad
{
    public class Startup
    {
        private const string StringConexao = @"Server=.\Sql2016;Database=LazyLoadArtigo;Integrated Security=True;ConnectRetryCount=0";

        public void ConfigureServices(IServiceCollection services)
        {
            // Adicionar LazyLoad como injeção de dependência
            services.AddEntityFrameworkProxies();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var db = new ExemploContext())
            {
                if (db.Database.EnsureCreated())
                {
                    db.Autores.Add(new Autor
                    {
                        Nome = "Bill Gates",
                        Livros = new[]
                         {
                             new Livro{ Descricao = "Microsoft Windows"},
                             new Livro{ Descricao = "Windows XP"}
                         }
                    });
                    db.SaveChanges();
                }

                // Buscar um autor
                var autorExplicito = db.Autores.FirstOrDefault(p => p.Id == 1);
                // Resultado SQL:
                // SELECT TOP(1) [p].[Id], [p].[Nome]
                // FROM[Autores] AS[p]
                // WHERE[p].[Id] = 1

                // Carregar os livros do autor de forma explícita
                db.Entry(autorExplicito)
                    .Collection(b => b.Livros)
                    .Load();

                // Resultado SQL:
                // exec sp_executesql N'SELECT [e].[Id], [e].[AutorId], [e].[DataLancamento], [e].[Descricao]
                // FROM[Livros] AS [e]
                // WHERE[e].[AutorId] = @__get_Item_0',N'@__get_Item_0 int',@__get_Item_0=1

                var autorAdiantado = db
                    .Autores
                    .Include(p => p.Livros)
                    .ToList();

                /*
                SELECT [p.Livros].[Id], [p.Livros].[AutorId], [p.Livros].[DataLancamento], [p.Livros].[Descricao]
                FROM [Livros] AS [p.Livros]
                INNER JOIN (
                    SELECT [p0].[Id]
                    FROM [Autores] AS [p0]
                ) AS [t] ON [p.Livros].[AutorId] = [t].[Id]
                ORDER BY [t].[Id]
                */

                // Carregamento LazyLoad (Muito mais simplificado)
                // basta apenas habilitar em seu DbContext
                var autorLazyLoad = db.Autores.ToList();
            }
            Console.ReadKey();
        }
    }

    public class Autor
    {
        public int Id { get; set; }
        public string Nome { get; set; }

        public virtual IEnumerable<Livro> Livros { get; set; }
    }

    public class Livro
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
        public DateTime DataLancamento { get; set; }

        public int AutorId { get; set; }
        public Autor Autor { get; set; }
    }

    public class ExemploContext : DbContext
    {
        private const string StringConexao
            = @"Server=.\Sql2016;Database=LazyLoadArtigo;Integrated Security=True;ConnectRetryCount=0";

        public DbSet<Autor> Autores { get; set; }
        public DbSet<Livro> Livros { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseLazyLoadingProxies();
            optionsBuilder.UseSqlServer(StringConexao);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Livro>()
                .HasOne(p => p.Autor)
                .WithMany(p => p.Livros)
                .HasForeignKey(p => p.AutorId);
        }
    }
}
