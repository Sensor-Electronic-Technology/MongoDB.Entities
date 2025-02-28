using System;

namespace MongoDB.Entities.Exceptions;

public class InvalidVersionValueException(string value)
    : Exception(string.Format(ErrorTexts.InvalidVersionValue, value));