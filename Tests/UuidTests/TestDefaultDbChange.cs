﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDB.Entities.Tests;

[TestClass]
public class DefaultDatabaseChangingUuid
{
    [TestMethod]
    public void throw_argument_null_exception()
    {
        Assert.ThrowsException<ArgumentNullException>(() => DB.ChangeDefaultDatabase(""));
    }

    [TestMethod]
    public void throw_invalid_operation_exception()
    {
        Assert.ThrowsException<InvalidOperationException>(() => DB.ChangeDefaultDatabase("db3"));
    }

    [TestMethod]
    public async Task returns_correct_database()
    {
        await DB.InitAsync("test1");
        await DB.InitAsync("test2");

        var defaultDb = DB.Database(default);
        var database = DB.Database("test2");

        DB.ChangeDefaultDatabase("test2");

        var bookDb = DB.Database<BookUuid>();

        Assert.AreEqual(database.DatabaseNamespace.DatabaseName, bookDb.DatabaseNamespace.DatabaseName);

        DB.ChangeDefaultDatabase(defaultDb.DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public async Task do_not_change_default_database_when_the_same()
    {
        await DB.InitAsync("test1");

        var defaultDb = DB.Database(default);
        var defaultDbName = DB.DatabaseName<AuthorUuid>();

        DB.ChangeDefaultDatabase(defaultDbName);

        var bookDb = DB.Database<BookUuid>();
        Assert.AreSame(defaultDb, bookDb);
    }
}