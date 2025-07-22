using System.Collections;
using System.Collections.Generic;
using AutoFixture.Xunit2;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Xunit;

namespace Forestry.Flo.Services.Common.Tests;

public class StringExtensionsTests
{
    [Theory]
    [AutoData]
    public void CanHandleJustLastName(string lastName)
    {
        var fullName = lastName.ParseFullName();

        Assert.Null(fullName.Title);
        Assert.Null(fullName.FirstName);
        Assert.Equal(lastName, fullName.LastName);
    }

    [Theory]
    [ClassData(typeof(TitlesTestData))]
    public void CanHandleTitleLastName(string title)
    {
        const string lastName = "Jones";

        var fullName = $"{title} {lastName}".ParseFullName();

        Assert.Equal(title, fullName.Title);
        Assert.Null(fullName.FirstName);
        Assert.Equal(lastName, fullName.LastName);
    }

    [Theory]
    [AutoData]
    public void CanHandleFirstNameLastName(string firstName, string lastName)
    {
        var fullName = $"{firstName} {lastName}".ParseFullName();

        Assert.Null(fullName.Title);
        Assert.Equal(firstName, fullName.FirstName);
        Assert.Equal(lastName, fullName.LastName);
    }

    [Theory]
    [AutoData]
    public void CanHandleFirstNameDoubleLastName(string firstName, string lastName1, string lastName2)
    {
        var fullName = $"{firstName} {lastName1} {lastName2}".ParseFullName();

        Assert.Null(fullName.Title);
        Assert.Equal(firstName, fullName.FirstName);
        Assert.Equal($"{lastName1} {lastName2}", fullName.LastName);
    }

    [Theory]
    [ClassData(typeof(TitlesTestData))]
    public void CanHandleTitleFirstNameLastName(string title)
    {
        const string firstName = "Bert";
        const string lastName = "Jones";

        var fullName = $"{title} {firstName} {lastName}".ParseFullName();

        Assert.Equal(title, fullName.Title);
        Assert.Equal(firstName, fullName.FirstName);
        Assert.Equal(lastName, fullName.LastName);
    }

    [Theory]
    [ClassData(typeof(TitlesTestData))]
    public void CanHandleTitleFirstNameDoubleLastName(string title)
    {
        const string firstName = "Bert";
        const string lastName1 = "Smith";
        const string lastName2 = "Jones";

        var fullName = $"{title} {firstName} {lastName1} {lastName2}".ParseFullName();

        Assert.Equal(title, fullName.Title);
        Assert.Equal(firstName, fullName.FirstName);
        Assert.Equal($"{lastName1} {lastName2}", fullName.LastName);
    }

    public class TitlesTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var item in typeof(TitlesEnum).GetDisplayNames())
            {
                yield return new object[] { item };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}