using System;
using Shouldly;
using Xunit;

namespace BlazorWebFormsComponents.Test;

/// <summary>
/// Tests for implicit string conversion operators on BWFC types.
/// These conversions enable Razor attributes like BackColor="White" and BorderWidth="1px"
/// to work without requiring @("value") wrappers.
/// </summary>
public class ImplicitConversionTests
{
	#region Unit Implicit Conversion Tests

	[Fact]
	public void Unit_ImplicitFromString_ParsesPixelValue()
	{
		Unit unit = "10px";
		unit.Value.ShouldBe(10);
		unit.Type.ShouldBe(Enums.UnitType.Pixel);
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesPercentValue()
	{
		Unit unit = "100%";
		unit.Value.ShouldBe(100);
		unit.Type.ShouldBe(Enums.UnitType.Percentage);
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesEmValue()
	{
		Unit unit = "1.5em";
		unit.Value.ShouldBe(1.5);
		unit.Type.ShouldBe(Enums.UnitType.Em);
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesPointValue()
	{
		Unit unit = "12pt";
		unit.Value.ShouldBe(12);
		unit.Type.ShouldBe(Enums.UnitType.Point);
	}

	[Fact]
	public void Unit_ImplicitFromString_NullReturnsEmpty()
	{
		Unit unit = (string)null;
		unit.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void Unit_ImplicitFromString_EmptyStringReturnsEmpty()
	{
		Unit unit = "";
		unit.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesPica()
	{
		Unit unit = "2pc";
		unit.Value.ShouldBe(2);
		unit.Type.ShouldBe(Enums.UnitType.Pica);
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesInches()
	{
		Unit unit = "1in";
		unit.Value.ShouldBe(1);
		unit.Type.ShouldBe(Enums.UnitType.Inch);
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesMillimeters()
	{
		Unit unit = "10mm";
		unit.Value.ShouldBe(10);
		unit.Type.ShouldBe(Enums.UnitType.Mm);
	}

	[Fact]
	public void Unit_ImplicitFromString_ParsesCentimeters()
	{
		Unit unit = "5cm";
		unit.Value.ShouldBe(5);
		unit.Type.ShouldBe(Enums.UnitType.Cm);
	}

	#endregion

	#region WebColor Implicit Conversion Tests

	[Fact]
	public void WebColor_ImplicitFromString_ParsesNamedColor()
	{
		WebColor color = "White";
		color.ToHtml().ShouldBe("White");
	}

	[Fact]
	public void WebColor_ImplicitFromString_ParsesHexColor()
	{
		WebColor color = "#FF0000";
		// Hex colors stay as hex in ToHtml() output
		color.ToHtml().ShouldBe("#FF0000");
	}

	[Fact]
	public void WebColor_ImplicitFromString_ParsesShortHex()
	{
		WebColor color = "#333333";
		color.ToHtml().ShouldBe("#333333");
	}

	[Fact]
	public void WebColor_ImplicitFromString_NullReturnsEmpty()
	{
		WebColor color = (string)null;
		color.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void WebColor_ImplicitFromString_EmptyStringReturnsEmpty()
	{
		WebColor color = "";
		color.IsEmpty.ShouldBeTrue();
	}

	#endregion

	#region FontUnit Implicit Conversion Tests

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesPointSize()
	{
		FontUnit fontUnit = "12pt";
		fontUnit.Type.ShouldBe(Enums.FontSize.AsUnit);
		fontUnit.Unit.Value.ShouldBe(12);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesMedium()
	{
		FontUnit fontUnit = "Medium";
		fontUnit.Type.ShouldBe(Enums.FontSize.Medium);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesSmall()
	{
		FontUnit fontUnit = "Small";
		fontUnit.Type.ShouldBe(Enums.FontSize.Small);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesLarge()
	{
		FontUnit fontUnit = "Large";
		fontUnit.Type.ShouldBe(Enums.FontSize.Large);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesXSmall()
	{
		FontUnit fontUnit = "X-Small";
		fontUnit.Type.ShouldBe(Enums.FontSize.XSmall);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesXLarge()
	{
		FontUnit fontUnit = "X-Large";
		fontUnit.Type.ShouldBe(Enums.FontSize.XLarge);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesXXSmall()
	{
		FontUnit fontUnit = "XX-Small";
		fontUnit.Type.ShouldBe(Enums.FontSize.XXSmall);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesXXLarge()
	{
		FontUnit fontUnit = "XX-Large";
		fontUnit.Type.ShouldBe(Enums.FontSize.XXLarge);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesSmaller()
	{
		FontUnit fontUnit = "Smaller";
		fontUnit.Type.ShouldBe(Enums.FontSize.Smaller);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_ParsesLarger()
	{
		FontUnit fontUnit = "Larger";
		fontUnit.Type.ShouldBe(Enums.FontSize.Larger);
	}

	[Fact]
	public void FontUnit_ImplicitFromString_NullReturnsEmpty()
	{
		FontUnit fontUnit = (string)null;
		fontUnit.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void FontUnit_ImplicitFromString_EmptyReturnsEmpty()
	{
		FontUnit fontUnit = "";
		fontUnit.IsEmpty.ShouldBeTrue();
	}

	[Fact]
	public void FontUnit_ImplicitFromString_CaseInsensitive()
	{
		FontUnit fontUnit = "medium";
		fontUnit.Type.ShouldBe(Enums.FontSize.Medium);
	}

	#endregion

	#region BoolString Implicit Conversion Tests

	[Fact]
	public void BoolString_ImplicitFromString_ParsesTrue()
	{
		BoolString value = "True";
		bool result = value;
		result.ShouldBeTrue();
	}

	[Fact]
	public void BoolString_ImplicitFromString_ParsesFalse()
	{
		BoolString value = "False";
		bool result = value;
		result.ShouldBeFalse();
	}

	[Fact]
	public void BoolString_ImplicitFromString_CaseInsensitiveTrue()
	{
		BoolString value = "true";
		bool result = value;
		result.ShouldBeTrue();
	}

	[Fact]
	public void BoolString_ImplicitFromString_CaseInsensitiveFalse()
	{
		BoolString value = "false";
		bool result = value;
		result.ShouldBeFalse();
	}

	[Fact]
	public void BoolString_ImplicitFromString_UppercaseTrue()
	{
		BoolString value = "TRUE";
		bool result = value;
		result.ShouldBeTrue();
	}

	[Fact]
	public void BoolString_ImplicitFromString_NullReturnsFalse()
	{
		BoolString value = (string)null;
		bool result = value;
		result.ShouldBeFalse();
	}

	[Fact]
	public void BoolString_ImplicitFromString_EmptyReturnsFalse()
	{
		BoolString value = "";
		bool result = value;
		result.ShouldBeFalse();
	}

	[Fact]
	public void BoolString_ImplicitFromBool_True()
	{
		BoolString value = true;
		bool result = value;
		result.ShouldBeTrue();
	}

	[Fact]
	public void BoolString_ImplicitFromBool_False()
	{
		BoolString value = false;
		bool result = value;
		result.ShouldBeFalse();
	}

	[Fact]
	public void BoolString_ImplicitToBool_InConditional()
	{
		BoolString value = "True";
		if (value)
		{
			// Should enter this block
		}
		else
		{
			Assert.Fail("BoolString should have been true");
		}
	}

	[Fact]
	public void BoolString_Equality_SameValueTrue()
	{
		BoolString a = "True";
		BoolString b = "True";
		(a == b).ShouldBeTrue();
	}

	[Fact]
	public void BoolString_Equality_DifferentValuesFalse()
	{
		BoolString a = "True";
		BoolString b = "False";
		(a != b).ShouldBeTrue();
	}

	[Fact]
	public void BoolString_ToString_ReturnsCorrectValue()
	{
		BoolString trueValue = "True";
		BoolString falseValue = "False";
		trueValue.ToString().ShouldBe("True");
		falseValue.ToString().ShouldBe("False");
	}

	[Fact]
	public void BoolString_Parse_InvalidValueThrows()
	{
		Should.Throw<FormatException>(() => BoolString.Parse("NotABoolean"));
	}

	[Fact]
	public void BoolString_TryParse_ReturnsTrueForValidValue()
	{
		BoolString.TryParse("True", out var result).ShouldBeTrue();
		((bool)result).ShouldBeTrue();
	}

	[Fact]
	public void BoolString_TryParse_ReturnsFalseForInvalidValue()
	{
		BoolString.TryParse("NotABoolean", out _).ShouldBeFalse();
	}

	#endregion
}
