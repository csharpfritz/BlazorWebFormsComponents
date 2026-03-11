using System;

namespace BlazorWebFormsComponents
{
	/// <summary>
	/// A struct that allows implicit conversion from string to bool, enabling Razor attributes like
	/// Font-Bold="True" to work without requiring @("True") wrappers.
	/// Handles case-insensitive "True"/"False" strings as expected by ASP.NET Web Forms.
	/// </summary>
	public readonly struct BoolString : IEquatable<BoolString>
	{
		private readonly bool _value;

		/// <summary>
		/// Initializes a new BoolString with the specified boolean value.
		/// </summary>
		public BoolString(bool value)
		{
			_value = value;
		}

		/// <summary>
		/// Gets the boolean value.
		/// </summary>
		public bool Value => _value;

		/// <summary>
		/// Parses a string value to a BoolString. Handles "True", "False" (case-insensitive).
		/// </summary>
		/// <param name="value">The string value to parse.</param>
		/// <returns>A BoolString representing the parsed value.</returns>
		/// <exception cref="FormatException">Thrown when the value cannot be parsed as a boolean.</exception>
		public static BoolString Parse(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return new BoolString(false);
			}

			// Handle case-insensitive True/False
			if (bool.TryParse(value, out var result))
			{
				return new BoolString(result);
			}

			throw new FormatException($"Cannot parse '{value}' as a boolean value. Expected 'True' or 'False'.");
		}

		/// <summary>
		/// Tries to parse a string value to a BoolString.
		/// </summary>
		public static bool TryParse(string value, out BoolString result)
		{
			if (string.IsNullOrEmpty(value))
			{
				result = new BoolString(false);
				return true;
			}

			if (bool.TryParse(value, out var boolResult))
			{
				result = new BoolString(boolResult);
				return true;
			}

			result = default;
			return false;
		}

		/// <summary>
		/// Implicitly converts a string to a BoolString.
		/// </summary>
		public static implicit operator BoolString(string value) => Parse(value);

		/// <summary>
		/// Implicitly converts a BoolString to a bool.
		/// </summary>
		public static implicit operator bool(BoolString value) => value._value;

		/// <summary>
		/// Implicitly converts a bool to a BoolString.
		/// </summary>
		public static implicit operator BoolString(bool value) => new BoolString(value);

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is BoolString other)
			{
				return _value == other._value;
			}
			if (obj is bool boolValue)
			{
				return _value == boolValue;
			}
			return false;
		}

		/// <inheritdoc/>
		public bool Equals(BoolString other) => _value == other._value;

		/// <inheritdoc/>
		public override int GetHashCode() => _value.GetHashCode();

		/// <inheritdoc/>
		public override string ToString() => _value.ToString();

		/// <summary>
		/// Compares two BoolString values for equality.
		/// </summary>
		public static bool operator ==(BoolString left, BoolString right) => left._value == right._value;

		/// <summary>
		/// Compares two BoolString values for inequality.
		/// </summary>
		public static bool operator !=(BoolString left, BoolString right) => left._value != right._value;
	}
}
