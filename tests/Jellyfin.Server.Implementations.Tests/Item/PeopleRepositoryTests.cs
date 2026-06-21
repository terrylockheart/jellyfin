using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class PeopleRepositoryTests
{
    [Fact]
    public void GetPeople_WithMovieItemType_FiltersActorsToMovieItems()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = CreateDbContext(dbOptions))
        {
            context.Database.EnsureCreated();

            var movie = new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Type = typeof(Movie).FullName!,
                Name = "Movie 1"
            };

            var series = new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Type = typeof(Series).FullName!,
                Name = "Series 1"
            };

            var movieActor = new People
            {
                Id = Guid.NewGuid(),
                Name = "Movie Actor",
                PersonType = PersonKind.Actor.ToString()
            };

            var seriesActor = new People
            {
                Id = Guid.NewGuid(),
                Name = "Series Actor",
                PersonType = PersonKind.Actor.ToString()
            };

            var movieMap = new PeopleBaseItemMap
            {
                ItemId = movie.Id,
                Item = movie,
                PeopleId = movieActor.Id,
                People = movieActor,
                ListOrder = 0,
            };

            var seriesMap = new PeopleBaseItemMap
            {
                ItemId = series.Id,
                Item = series,
                PeopleId = seriesActor.Id,
                People = seriesActor,
                ListOrder = 0,
            };

            movie.Peoples = new List<PeopleBaseItemMap> { movieMap };
            series.Peoples = new List<PeopleBaseItemMap> { seriesMap };
            movieActor.BaseItems = new List<PeopleBaseItemMap> { movieMap };
            seriesActor.BaseItems = new List<PeopleBaseItemMap> { seriesMap };

            context.AddRange(movie, series, movieActor, seriesActor, movieMap, seriesMap);
            context.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(() => CreateDbContext(dbOptions));

        var itemTypeLookup = new Mock<IItemTypeLookup>();
        itemTypeLookup.SetupGet(x => x.BaseItemKindNames).Returns(new Dictionary<BaseItemKind, string>
        {
            [BaseItemKind.Movie] = typeof(Movie).FullName!,
            [BaseItemKind.Series] = typeof(Series).FullName!,
            [BaseItemKind.Person] = typeof(Person).FullName!,
        });

        var repository = new PeopleRepository(factory.Object, itemTypeLookup.Object);

        var result = repository.GetPeople(new InternalPeopleQuery(
            [PersonKind.Actor.ToString()],
            Array.Empty<string>())
        {
            IncludeItemTypes = [BaseItemKind.Movie]
        });

        Assert.Single(result.Items);
        Assert.Equal("Movie Actor", result.Items[0].Name);
    }

    private static JellyfinDbContext CreateDbContext(DbContextOptions<JellyfinDbContext> options)
    {
        return new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
