﻿using System.Collections.Generic;
using Application.Extensions;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void ToSeoUrl_ReturnsNull_WhenValueNull()
    {
        string? value = null;

        value.ToSeoUrl().Should().BeNull();
    }

    [Fact]
    public void ToSeoUrl_ReturnsEmptyString_WhenValueEmptyString()
    {
        const string value = "";

        value.ToSeoUrl().Should().Be("");
    }

    [Fact]
    public void ToSeoUrl_ReturnsEmptyString_WhenValueWhitespace()
    {
        const string value = "   ";

        value.ToSeoUrl().Should().Be("");
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCase_WhenValuePascalCase()
    {
        const string value = "FindATuitionPartner";

        value.ToSeoUrl().Should().Be("find-a-tuition-partner");
    }

    [Fact]
    public void ToSeoUrl_ReturnsTrimmedKebabCase_WhenValueContainsWhitespace()
    {
        const string value = " FindATuitionPartner ";

        value.ToSeoUrl().Should().Be("find-a-tuition-partner");
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCaseWithoutSplitting_WhenValueContainsUppercase()
    {
        const string value = "FindAnNTPApprovedTuitionPartner";

        value.ToSeoUrl().Should().Be("find-an-ntp-approved-tuition-partner");
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCase_WhenValueKebabCase()
    {
        const string value = "find-a-tuition-partner";

        value.ToSeoUrl().Should().Be("find-a-tuition-partner");
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCase_WhenValueCamelCase()
    {
        const string value = "searchId";

        value.ToSeoUrl().Should().Be("search-id");
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCase_WhenHasUnderscore()
    {
        const string value = "search_ Id";

        value.ToSeoUrl().Should().Be("search-id");
    }

    [Theory]
    [InlineData("m2r Education", "m-2-r-education")]
    [InlineData("KeyStage1-Science", "key-stage-1-science")]
    [InlineData("KeyStage1Science", "key-stage-1-science")]
    [InlineData("Keystage1science", "keystage-1-science")]
    [InlineData("KeyStage123Science", "key-stage-123-science")]
    [InlineData("m123r Education", "m-123-r-education")]
    public void ToSeoUrl_ReturnsKebabCase_WhenValueCamelCase_WithNumbers(string camel, string kebab)
    {
        camel.ToSeoUrl().Should().Be(kebab);
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCase_WhenValueCamelCase_WithHypen()
    {
        const string value = "PET-Xi Training Limited";

        value.ToSeoUrl().Should().Be("pet-xi-training-limited");
    }

    [Fact]
    public void ToSeoUrl_ReturnsKebabCase_WhenValueCamelCase_WithAmpersand()
    {
        const string value = "CER, Monarch Education & Sugarman Education (Parent- Affinity Workforce Solutions)";

        value.ToSeoUrl().Should().Be("cer-monarch-education-sugarman-education-(parent-affinity-workforce-solutions)");
    }

    [Theory]
    [InlineData("apostrophe's", "apostrophes")]
    [InlineData("special!\"£$%^&*+=chars", "specialchars")]
    [InlineData("more[];:@#,.<>?special", "morespecial")]
    [InlineData("chars-for-aspnet-operation-(){}", "chars-for-aspnet-operation-(){}")]
    [InlineData("unicode६symbols你好", "unicode-symbols")]
    public void ToSeoUrl_ReturnsKebabCase_Without_UrlEncoded_Characters(string name, string seoName)
    {
        name.ToSeoUrl().Should().Be(seoName);
    }

    [Theory]
    [InlineData("Find/Location", "find/location")]
    [InlineData("find/location", "find/location")]
    [InlineData("FindPage/LocationSearch", "find-page/location-search")]
    [InlineData("find-page/location-search", "find-page/location-search")]
    [InlineData("Find/FindAnNTPApprovedTuitionPartner", "find/find-an-ntp-approved-tuition-partner")]
    [InlineData("Find/FindATuitionPartner", "find/find-a-tuition-partner")]
    [InlineData("Find/FindATuitionPartner ", "find/find-a-tuition-partner")]
    [InlineData("Find/findATuitionPartner", "find/find-a-tuition-partner")]
    [InlineData("tuition-partner/A Tuition Co", "tuition-partner/a-tuition-co")]
    public void ToSeoUrl_ReturnsKebabCase_WhenDirectory(string camel, string kebab)
    {
        camel.ToSeoUrl().Should().Be(kebab);
    }

    [Theory]
    [InlineData("KeyStage1-Science", "key-stage-1-science")]
    [InlineData("KeyStage2 Literature", "key-stage-2-literature")]
    [InlineData("KeyStage3 Modern Foreign Languages", "key-stage-3-modern-foreign-languages")]
    public void With_spaces(string value, string expected)
    {
        value.ToSeoUrl().Should().Be(expected);
    }

    [Theory]
    [InlineData("KeyStage1", "key-stage-1")]
    public void With_trailing_digit(string value, string expected)
    {
        value.ToSeoUrl().Should().Be(expected);
    }

    [Fact]
    public void DisplayList_Null()
    {
        IEnumerable<string>? list = null;
        list.DisplayList().Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(new string[] { }, "")]
    [InlineData(new string[] { "test" }, "test")]
    [InlineData(new string[] { "test1 test2" }, "test1 test2")]
    [InlineData(new string[] { "test1", "test2" }, "test1 and test2")]
    [InlineData(new string[] { "test1", "test2", "test3" }, "test1, test2 and test3")]
    public void DisplayList_WithData(string[] stringArray, string output)
    {
        List<string> list = new List<string>(stringArray);
        list.DisplayList().Should().Be(output);
    }
}