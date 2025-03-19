using System;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Exceptions;

namespace MongoDB.Entities;

public struct DocumentVersion : IComparable<DocumentVersion> {
    private const char VERSION_SPLIT_CHAR = '.';

    private const int MAX_LENGTH = 3;

    public int Major { get; set; }

    public int Minor { get; set; }

    public int Revision { get; set; }

    static DocumentVersion() {
        try {
            BsonSerializer.RegisterSerializer(typeof(DocumentVersion), new DocumentVersionSerializer());
        } catch (Exception) { }
    }

    public DocumentVersion(string version) {
        string[] versionParts = version.Split(VERSION_SPLIT_CHAR);

        if (versionParts.Length != MAX_LENGTH) {
            throw new VersionStringToLongException(version);
        }

        Major = ParseVersionPart(versionParts[0]);

        Minor = ParseVersionPart(versionParts[1]);

        Revision = ParseVersionPart(versionParts[2]);
    }

    public DocumentVersion(int major, int minor, int revision) {
        Major = major;
        Minor = minor;
        Revision = revision;
    }

    public static DocumentVersion Default() {
        return default(DocumentVersion);
    }

    public static DocumentVersion Empty() {
        return new DocumentVersion(-1, 0, 0);
    }

    public DocumentVersion IncrementMajor() {
        Major=Major<0 ? 0:Major;
        Major+=1;
        Minor = 0;
        Revision = 0;
        return this;
    }
    public DocumentVersion Increment() {
        if (Major < 0) {
            return this.IncrementMajor();
        }
        if (Revision < 9) {
            Revision++;
            return this;
        }

        if (Minor < 9) {
            Minor++;
            Revision = 0;
            return this;
        }
        return this.IncrementMajor();
    }
    
    public DocumentVersion DecrementMajor() {
        Major=Major is > 0 and 1 ? 0:--Major;
        return this;
    }

    public DocumentVersion Decrement() {
        if (Revision >= 1) {
            Revision--;
            return this;
        }
        if (Minor >= 1) {
            Minor--;
            Revision = 9;
            return this;
        }
        return this.DecrementMajor();
    }

    public static implicit operator DocumentVersion(string version) {
        return new DocumentVersion(version);
    }

    public static implicit operator string(DocumentVersion documentVersion) {
        return documentVersion.ToString();
    }

    public override string ToString() {
        return $"{Major}.{Minor}.{Revision}";
    }

    public int CompareTo(DocumentVersion other) {
        if (Equals(other)) {
            return 0;
        }

        return this > other ? 1 : -1;
    }

    public static bool operator ==(DocumentVersion a, DocumentVersion b) {
        return a.Equals(b);
    }

    public static bool operator !=(DocumentVersion a, DocumentVersion b) {
        return !(a == b);
    }

    public static bool operator >(DocumentVersion a, DocumentVersion b) {
        return a.Major > b.Major
               || (a.Major == b.Major && a.Minor > b.Minor)
               || (a.Major == b.Major && a.Minor == b.Minor && a.Revision > b.Revision);
    }

    public static bool operator <(DocumentVersion a, DocumentVersion b) {
        return a != b && !(a > b);
    }

    public static bool operator <=(DocumentVersion a, DocumentVersion b) {
        return a == b || a < b;
    }

    public static bool operator >=(DocumentVersion a, DocumentVersion b) {
        return a == b || a > b;
    }

    public bool Equals(DocumentVersion other) {
        return other.Major == Major && other.Minor == Minor && other.Revision == Revision;
    }

    public override bool Equals(object? obj) {
        if (obj is null) {
            return false;
        }

        if (obj.GetType() != typeof(DocumentVersion)) {
            return false;
        }

        return Equals((DocumentVersion)obj);
    }

    public override int GetHashCode() {
        unchecked {
            int result = Major;
            result = (result * 397) ^ Minor;
            result = (result * 397) ^ Revision;
            return result;
        }
    }

    private static int ParseVersionPart(string value) {
        string revisionString = value;
        if (!int.TryParse(revisionString, out var target)) {
            throw new InvalidVersionValueException(revisionString);
        }

        return target;
    }
}