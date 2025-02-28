using System;

namespace MongoDB.Entities.Exceptions;

public class VersionStringToLongException(string version)
    : Exception(string.Format(ErrorTexts.VersionStringToLong, version));