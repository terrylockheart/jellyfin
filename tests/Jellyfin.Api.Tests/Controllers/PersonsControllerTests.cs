using System;
using System.Security.Claims;
using Jellyfin.Data.Enums;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class PersonsControllerTests
{
    [Fact]
    public void GetPersons_WithMovieItemType_PassesFilterToLibraryQuery()
    {
        var libraryManager = new Mock<ILibraryManager>();
        var dtoService = new Mock<IDtoService>();
        var userManager = new Mock<IUserManager>();

        InternalPeopleQuery? capturedQuery = null;
        libraryManager
            .Setup(m => m.GetPeopleItems(It.IsAny<InternalPeopleQuery>()))
            .Callback<InternalPeopleQuery>(query => capturedQuery = query)
            .Returns(new QueryResult<BaseItem>
            {
                StartIndex = 0,
                TotalRecordCount = 0,
                Items = Array.Empty<BaseItem>()
            });

        var controller = new PersonsController(libraryManager.Object, dtoService.Object, userManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[]
                        {
                            new Claim(InternalClaimTypes.UserId, Guid.NewGuid().ToString("N"))
                        }))
                }
            }
        };

        controller.GetPersons(
            startIndex: null,
            limit: null,
            searchTerm: null,
            nameStartsWith: null,
            nameLessThan: null,
            nameStartsWithOrGreater: null,
            fields: Array.Empty<ItemFields>(),
            filters: Array.Empty<ItemFilter>(),
            isFavorite: null,
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: Array.Empty<ImageType>(),
            excludePersonTypes: Array.Empty<string>(),
            personTypes: [PersonKind.Actor.ToString()],
            includeItemTypes: [BaseItemKind.Movie],
            parentId: null,
            appearsInItemId: null,
            userId: null,
            enableImages: false);

        Assert.NotNull(capturedQuery);
        Assert.Single(capturedQuery!.IncludeItemTypes);
        Assert.Equal(BaseItemKind.Movie, capturedQuery.IncludeItemTypes[0]);
        Assert.Single(capturedQuery.PersonTypes);
        Assert.Equal(PersonKind.Actor.ToString(), capturedQuery.PersonTypes[0]);
        Assert.Empty(capturedQuery.ExcludePersonTypes);
    }
}
