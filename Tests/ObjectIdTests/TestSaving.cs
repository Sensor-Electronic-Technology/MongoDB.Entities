using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Entities.Tests.Models;

namespace MongoDB.Entities.Tests;

[TestClass]
public class SavingObjectId
{
    [TestMethod]
    public async Task saving_new_document_returns_an_id()
    {
        var book = new BookObjectId { Title = "Test" };
        await book.SaveAsync();
        var idEmpty = book.ID == ObjectId.Empty;
        Assert.IsFalse(idEmpty);
    }

    [TestMethod]
    public async Task saved_book_has_correct_title()
    {
        var book = new BookObjectId { Title = "Test" };
        await book.SaveAsync();
        var title = book.Queryable().Where(b => b.ID == book.ID).Select(b => b.Title).SingleOrDefault();
        Assert.AreEqual("Test", title);
    }

    [TestMethod]
    public async Task insert_single_ObjectId()
    {
        var db = new DBContext(modifiedBy: new());

        var author = new AuthorObjectId { Name = "test" };
        await db.InsertAsync(author);

        var res = await db.Find<AuthorObjectId>().MatchID(author.ID).ExecuteAnyAsync();

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task insert_batch()
    {
        var guid = Guid.NewGuid().ToString();

        var db = new DBContext(modifiedBy: new());

        var author1 = new AuthorObjectId { Name = guid };
        var author2 = new AuthorObjectId { Name = guid };

        await db.InsertAsync(new[] { author1, author2 });

        var res = await db.Find<AuthorObjectId>()
                          .Match(a => a.Name == guid)
                          .ExecuteAsync();

        Assert.AreEqual(2, res.Count);
    }

    [TestMethod]
    public async Task created_on_property_works()
    {
        var author = new AuthorObjectId { Name = "test" };
        await author.SaveAsync();

        var res = (await DB.Find<AuthorObjectId, DateTime>()
                           .Match(a => a.ID == author.ID)
                           .Project(a => a.CreatedOn)
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(res.ToLongTimeString(), author.CreatedOn.ToLongTimeString());
        Assert.IsTrue(DateTime.UtcNow.Subtract(res).TotalSeconds <= 5);
    }

    [TestMethod]
    public async Task save_partially_single_include()
    {
        var book = new BookObjectId { Title = "test book", Price = 100 };

        await book.SaveOnlyAsync(b => new { b.Title });

        var res = await DB.Find<BookObjectId>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(0, res!.Price);
        Assert.AreEqual("test book", res.Title);

        res.Price = 200;

        await res.SaveOnlyAsync(b => new { b.Price });

        res = await DB.Find<BookObjectId>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual(200, res!.Price);
    }

    [TestMethod]
    public async Task save_partially_single_include_string()
    {
        var book = new BookObjectId { Title = "test book", Price = 100 };

        await book.SaveOnlyAsync(new List<string> { "Title" });

        var res = await DB.Find<BookObjectId>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(0, res!.Price);
        Assert.AreEqual("test book", res.Title);

        res.Price = 200;

        await res.SaveOnlyAsync(new List<string> { "Price" });

        res = await DB.Find<BookObjectId>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual(200, res!.Price);
    }

    [TestMethod]
    public async Task save_partially_batch_include()
    {
        var books = new[]
        {
            new BookObjectId { Title = "one", Price = 100 },
            new BookObjectId { Title = "two", Price = 200 }
        };

        await books.SaveOnlyAsync(b => new { b.Title });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await DB.Find<BookObjectId>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(0, res[0].Price);
        Assert.AreEqual(0, res[1].Price);
        Assert.AreEqual("one", res[0].Title);
        Assert.AreEqual("two", res[1].Title);
    }

    [TestMethod]
    public async Task save_partially_batch_include_string()
    {
        var books = new[]
        {
            new BookObjectId { Title = "one", Price = 100 },
            new BookObjectId { Title = "two", Price = 200 }
        };

        await books.SaveOnlyAsync(new List<string> { "Title" });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await DB.Find<BookObjectId>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(0, res[0].Price);
        Assert.AreEqual(0, res[1].Price);
        Assert.AreEqual("one", res[0].Title);
        Assert.AreEqual("two", res[1].Title);
    }

    [TestMethod]
    public async Task save_partially_single_exclude()
    {
        var book = new BookObjectId { Title = "test book", Price = 100 };

        await book.SaveExceptAsync(b => new { b.Title });

        var res = await DB.Find<BookObjectId>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(100, res!.Price);
        Assert.AreEqual(null, res.Title);

        res.Title = "updated";

        await res.SaveExceptAsync(b => new { b.Price });

        res = await DB.Find<BookObjectId>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual("updated", res!.Title);
    }

    [TestMethod]
    public async Task save_partially_single_exclude_string()
    {
        var book = new BookObjectId { Title = "test book", Price = 100 };

        await book.SaveExceptAsync(new List<string> { "Title" });

        var res = await DB.Find<BookObjectId>().MatchID(book.ID).ExecuteSingleAsync();

        Assert.AreEqual(100, res!.Price);
        Assert.AreEqual(null, res.Title);

        res.Title = "updated";

        await res.SaveExceptAsync(new List<string> { "Price" });

        res = await DB.Find<BookObjectId>().MatchID(res.ID).ExecuteSingleAsync();

        Assert.AreEqual("updated", res!.Title);
    }

    [TestMethod]
    public async Task save_partially_batch_exclude()
    {
        var books = new[]
        {
            new BookObjectId { Title = "one", Price = 100 },
            new BookObjectId { Title = "two", Price = 200 }
        };

        await books.SaveExceptAsync(b => new { b.Title });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await DB.Find<BookObjectId>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(100, res[0].Price);
        Assert.AreEqual(200, res[1].Price);
        Assert.AreEqual(null, res[0].Title);
        Assert.AreEqual(null, res[1].Title);
    }

    [TestMethod]
    public async Task save_partially_batch_exclude_string()
    {
        var books = new[]
        {
            new BookObjectId { Title = "one", Price = 100 },
            new BookObjectId { Title = "two", Price = 200 }
        };

        await books.SaveExceptAsync(new List<string> { "Title" });
        var ids = books.Select(b => b.ID).ToArray();

        var res = await DB.Find<BookObjectId>()
                          .Match(b => ids.Contains(b.ID))
                          .Sort(b => b.ID, Order.Ascending)
                          .ExecuteAsync();

        Assert.AreEqual(100, res[0].Price);
        Assert.AreEqual(200, res[1].Price);
        Assert.AreEqual(null, res[0].Title);
        Assert.AreEqual(null, res[1].Title);
    }

    [TestMethod]
    public async Task save_preserving_upsert()
    {
        var book = new BookObjectId { Title = "Original Title", Price = 123.45m, DontSaveThis = 111 };

        book.ID = (ObjectId)book.GenerateNewID();
        book.Title = "updated title";
        book.Price = 543.21m;

        await book.SavePreservingAsync();

        book = await DB.Find<BookObjectId>().OneAsync(book.ID);

        Assert.AreEqual("updated title", book!.Title);
        Assert.AreEqual(543.21m, book.Price);
        Assert.AreEqual(default, book.DontSaveThis);
    }

    [TestMethod]
    public async Task save_preserving()
    {
        var book = new BookObjectId { Title = "Original Title", Price = 123.45m, DontSaveThis = 111 };
        await book.SaveAsync();

        book.Title = "updated title";
        book.Price = 543.21m;

        await book.SavePreservingAsync();

        book = await DB.Find<BookObjectId>().OneAsync(book.ID);

        Assert.AreEqual("updated title", book!.Title);
        Assert.AreEqual(543.21m, book.Price);
        Assert.AreEqual(default, book.DontSaveThis);
    }

    [TestMethod]
    public async Task save_preserving_inverse_attribute()
    {
        var book = new BookObjectId
        {
            Title = "original", //dontpreserve
            Price = 100,        //dontpreserve
            PriceDbl = 666,
            MainAuthor = new(ObjectId.GenerateNewId())
        };
        await book.SaveAsync();

        book.Title = "updated";
        book.Price = 111;
        book.PriceDbl = 999;
        book.MainAuthor = null!;

        await book.SavePreservingAsync();

        var res = await DB.Find<BookObjectId>().OneAsync(book.ID);

        Assert.AreEqual(res!.Title, book.Title);
        Assert.AreEqual(res.Price, book.Price);
        Assert.AreEqual(res.PriceDbl, 666);
        Assert.IsFalse(res.MainAuthor.ID == ObjectId.Empty);
    }

    [TestMethod]
    public async Task save_preserving_attribute()
    {
        var author = new AuthorObjectId
        {
            Age = 123,
            Name = "initial name",
            FullName = "initial fullname",
            Birthday = DateTime.UtcNow.ToDate()
        };
        await author.SaveAsync();

        author.Name = "updated author name";
        author.Age = 666;                         //preserve
        author.Age2 = 400;                        //preserve
        author.Birthday = new(DateTime.MinValue); //preserve
        author.FullName = null;
        author.BestSeller = new(ObjectId.GenerateNewId());

        await author.SavePreservingAsync();

        var res = await DB.Find<AuthorObjectId>().OneAsync(author.ID);

        Assert.AreEqual("updated author name", res!.Name);
        Assert.AreEqual(123, res.Age);
        Assert.AreEqual(default, res.Age2);
        Assert.AreNotEqual(DateTime.MinValue, res.Birthday.DateTime);
        Assert.AreEqual("initial fullname", res.FullName);
        Assert.AreEqual(author.BestSeller.ID, res.BestSeller.ID);
    }

    [TestMethod]
    public async Task embedding_non_ObjectId_returns_correct_document()
    {
        var book = new BookObjectId
        {
            Title = "Test",
            Review = new() { Stars = 5, Reviewer = "enercd" }
        };
        await book.SaveAsync();
        var res = book.Queryable()
                      .Where(b => b.ID == book.ID)
                      .Select(b => b.Review.Reviewer)
                      .SingleOrDefault();
        Assert.AreEqual(book.Review.Reviewer, res);
    }

    [TestMethod]
    public async Task embedding_with_ToDocument_returns_correct_doc()
    {
        var book = new BookObjectId { Title = "Test" };
        var author = new AuthorObjectId { Name = "ewtdrcd" };
        book.RelatedAuthor = author.ToDocument();
        await book.SaveAsync();
        var res = book.Queryable()
                      .Where(b => b.ID == book.ID)
                      .Select(b => b.RelatedAuthor.Name)
                      .SingleOrDefault();
        Assert.AreEqual(book.RelatedAuthor.Name, res);
    }

    [TestMethod]
    public async Task embedding_with_ToDocument_returns_blank_id()
    {
        var book = new BookObjectId { Title = "Test" };
        var author = new AuthorObjectId { Name = "Test Author" };
        book.RelatedAuthor = author.ToDocument();
        await book.SaveAsync();
        var res = book.Queryable()
                      .Where(b => b.ID == book.ID)
                      .Select(b => b.RelatedAuthor.ID)
                      .SingleOrDefault();
        Assert.AreEqual(book.RelatedAuthor.ID, res);
    }

    [TestMethod]
    public async Task embedding_with_ToDocuments_Arr_returns_correct_docs()
    {
        var book = new BookObjectId { Title = "Test" };
        await book.SaveAsync();
        var author1 = new AuthorObjectId { Name = "ewtrcd1" };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = "ewtrcd2" };
        await author2.SaveAsync();
        book.OtherAuthors = new[] { author1, author2 }.ToDocuments();
        await book.SaveAsync();
        var authors = book.Queryable()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.OtherAuthors).Single();
        Assert.AreEqual(authors.Length, 2);
        Assert.AreEqual(author2.Name, authors[1].Name);
        Assert.AreEqual(book.OtherAuthors[0].ID, authors[0].ID);
    }

    [TestMethod]
    public async Task embedding_with_ToDocuments_IEnumerable_returns_correct_docs()
    {
        var book = new BookObjectId { Title = "Test" };
        await book.SaveAsync();
        var author1 = new AuthorObjectId { Name = "ewtrcd1" };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = "ewtrcd2" };
        await author2.SaveAsync();
        var list = new List<AuthorObjectId> { author1, author2 };
        book.OtherAuthors = list.ToDocuments().ToArray();
        await book.SaveAsync();
        var authors = book.Queryable()
                          .Where(b => b.ID == book.ID)
                          .Select(b => b.OtherAuthors).Single();
        Assert.AreEqual(authors.Length, 2);
        Assert.AreEqual(author2.Name, authors[1].Name);
        Assert.AreEqual(book.OtherAuthors[0].ID, authors[0].ID);
    }

    [TestMethod]
    public async Task find_with_ignore_global_filter()
    {
        var db = new MyDBObjectId();

        var guid = Guid.NewGuid().ToString();

        await new[]
        {
            new AuthorObjectId { Name = guid, Age = 200 },
            new AuthorObjectId { Name = guid, Age = 200 },
            new AuthorObjectId { Name = guid, Age = 111 }
        }.SaveAsync();

        var res = await db.Find<AuthorObjectId>()
                          .Match(a => a.Name == guid)
                          .IgnoreGlobalFilters()
                          .ExecuteAsync();

        Assert.AreEqual(3, res.Count);
    }

    [TestMethod]
    public async Task queryable_with_global_filter()
    {
        var db = new MyDBObjectId();

        var guid = Guid.NewGuid().ToString();

        await new[]
        {
            new AuthorObjectId { Name = guid, Age = 200 },
            new AuthorObjectId { Name = guid, Age = 200 },
            new AuthorObjectId { Name = guid, Age = 111 }
        }.SaveAsync();

        var res = await db.Queryable<AuthorObjectId>()
                          .Where(a => a.Name == guid)
                          .ToListAsync();

        Assert.AreEqual(1, res.Count);
    }

    [TestMethod]
    public async Task global_filter_for_base_class()
    {
        var guid = Guid.NewGuid().ToString();

        var db = new MyBaseEntityDB();

        var flowers = new[]
        {
            new FlowerObjectId { Name = guid, CreatedBy = "xyz" },
            new FlowerObjectId { Name = guid },
            new FlowerObjectId { Name = guid }
        };

        await db.SaveAsync(flowers);

        var res = await db.Find<FlowerObjectId>().Match(f => f.Name == guid).ExecuteAsync();

        Assert.AreEqual(1, res.Count);
    }

    [TestMethod]
    public async Task global_filter_for_interface_prepend()
    {
        var db = new MyDBFlower(prepend: true);

        var guid = Guid.NewGuid().ToString();

        var flowers = new[]
        {
            new FlowerObjectId { Name = guid, IsDeleted = true },
            new FlowerObjectId { Name = guid },
            new FlowerObjectId { Name = guid }
        };

        await db.SaveAsync(flowers);

        var res = await db.Find<FlowerObjectId>().Match(f => f.Name == guid).ExecuteAsync();

        Assert.AreEqual(2, res.Count);
    }

    [TestMethod]
    public async Task find_by_lambda_returns_correct_documents()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorObjectId { Name = guid };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = guid };
        await author2.SaveAsync();

        var res = await DB.Find<AuthorObjectId>().ManyAsync(a => a.Name == guid);

        Assert.AreEqual(2, res.Count);
    }

    [TestMethod]
    public async Task find_by_id_returns_correct_document()
    {
        var book1 = new BookObjectId { Title = "fbircdb1" };
        await book1.SaveAsync();
        var book2 = new BookObjectId { Title = "fbircdb2" };
        await book2.SaveAsync();

        var res1 = await DB.Find<BookObjectId>().OneAsync(ObjectId.GenerateNewId());
        var res2 = await DB.Find<BookObjectId>().OneAsync(book2.ID);

        Assert.AreEqual(null, res1);
        Assert.AreEqual(book2.ID, res2!.ID);
    }

    [TestMethod]
    public async Task find_by_filter_basic_returns_correct_documents()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorObjectId { Name = guid };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = guid };
        await author2.SaveAsync();

        var res = await DB.Find<AuthorObjectId>().ManyAsync(f => f.Eq(a => a.Name, guid));

        Assert.AreEqual(2, res.Count);
    }

    [TestMethod]
    public async Task find_by_filter_single()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorObjectId { Name = guid };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = guid };
        await author2.SaveAsync();

        var res = await DB
                        .Find<AuthorObjectId>()
                        .Match(f => f.Eq(a => a.Name, guid))
                        .ExecuteFirstAsync();

        Assert.AreEqual(author1.ID, res!.ID);
    }

    [TestMethod]
    public async Task find_by_filter_any()
    {
        var guid = Guid.NewGuid().ToString();
        var author1 = new AuthorObjectId { Name = guid };
        await author1.SaveAsync();
        var author2 = new AuthorObjectId { Name = guid };
        await author2.SaveAsync();

        var res = await DB
                        .Find<AuthorObjectId>()
                        .Match(f => f.Eq(a => a.Name, guid))
                        .ExecuteAnyAsync();

        Assert.IsTrue(res);
    }

    [TestMethod]
    public async Task find_by_multiple_match_methods()
    {
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorObjectId { Name = "a", Age = 10, Surname = guid };
        await one.SaveAsync();
        var two = new AuthorObjectId { Name = "b", Age = 20, Surname = guid };
        await two.SaveAsync();
        var three = new AuthorObjectId { Name = "c", Age = 30, Surname = guid };
        await three.SaveAsync();
        var four = new AuthorObjectId { Name = "d", Age = 40, Surname = guid };
        await four.SaveAsync();

        var res = await DB.Find<AuthorObjectId>()
                          .Match(a => a.Age > 10)
                          .Match(a => a.Surname == guid)
                          .ExecuteAsync();

        Assert.AreEqual(3, res.Count);
        Assert.IsFalse(res.Any(a => a.Age == 10));
    }

    [TestMethod]
    public async Task find_by_filter_returns_correct_documents()
    {
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorObjectId { Name = "a", Age = 10, Surname = guid };
        await one.SaveAsync();
        var two = new AuthorObjectId { Name = "b", Age = 20, Surname = guid };
        await two.SaveAsync();
        var three = new AuthorObjectId { Name = "c", Age = 30, Surname = guid };
        await three.SaveAsync();
        var four = new AuthorObjectId { Name = "d", Age = 40, Surname = guid };
        await four.SaveAsync();

        var res = await DB.Find<AuthorObjectId>()
                          .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                          .Sort(a => a.Age, Order.Descending)
                          .Sort(a => a.Name, Order.Descending)
                          .Skip(1)
                          .Limit(1)
                          .Project(p => p.Include("Name").Include("Surname"))
                          .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                          .ExecuteAsync();

        Assert.AreEqual(three.Name, res[0].Name);
    }

    class Test
    {
        public string Tester { get; set; }
    }

    [TestMethod]
    public async Task find_with_projection_to_custom_type_works()
    {
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorObjectId { Name = "a", Age = 10, Surname = guid };
        await one.SaveAsync();
        var two = new AuthorObjectId { Name = "b", Age = 20, Surname = guid };
        await two.SaveAsync();
        var three = new AuthorObjectId { Name = "c", Age = 30, Surname = guid };
        await three.SaveAsync();
        var four = new AuthorObjectId { Name = "d", Age = 40, Surname = guid };
        await four.SaveAsync();

        var res = (await DB.Find<AuthorObjectId, Test>()
                           .Match(f => f.Where(a => a.Surname == guid) & f.Gt(a => a.Age, 10))
                           .Sort(a => a.Age, Order.Descending)
                           .Sort(a => a.Name, Order.Descending)
                           .Skip(1)
                           .Limit(1)
                           .Project(a => new() { Tester = a.Name })
                           .Option(o => o.MaxTime = TimeSpan.FromSeconds(1))
                           .ExecuteAsync())
            .FirstOrDefault();

        Assert.AreEqual(three.Name, res!.Tester);
    }

    [TestMethod]
    public async Task find_with_exclusion_projection_works()
    {
        var author = new AuthorObjectId
        {
            Name = "name",
            Surname = "sername",
            Age = 22,
            FullName = "fullname"
        };
        await author.SaveAsync();

        var res = (await DB.Find<AuthorObjectId>()
                           .Match(a => a.ID == author.ID)
                           .ProjectExcluding(a => new { a.Age, a.Name })
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(author.FullName, res.FullName);
        Assert.AreEqual(author.Surname, res.Surname);
        Assert.IsTrue(res.Age == default);
        Assert.IsTrue(res.Name == default);
    }

    [TestMethod]
    public async Task find_with_aggregation_pipeline_returns_correct_docs()
    {
        var guid = Guid.NewGuid().ToString();
        var one = new AuthorObjectId { Name = "a", Age = 10, Surname = guid };
        await one.SaveAsync();
        var two = new AuthorObjectId { Name = "b", Age = 20, Surname = guid };
        await two.SaveAsync();
        var three = new AuthorObjectId { Name = "c", Age = 30, Surname = guid };
        await three.SaveAsync();
        var four = new AuthorObjectId { Name = "d", Age = 40, Surname = guid };
        await four.SaveAsync();

        var res = await DB.Fluent<AuthorObjectId>()
                          .Match(a => a.Surname == guid && a.Age > 10)
                          .SortByDescending(a => a.Age)
                          .ThenByDescending(a => a.Name)
                          .Skip(1)
                          .Limit(1)
                          .Project(a => new { Test = a.Name })
                          .FirstOrDefaultAsync();

        Assert.AreEqual(three.Name, res.Test);
    }

    [TestMethod]
    public async Task find_with_aggregation_expression_works()
    {
        var guid = Guid.NewGuid().ToString();
        var author = new AuthorObjectId { Name = "a", Age = 10, Age2 = 11, Surname = guid };
        await author.SaveAsync();

        var res = (await DB.Find<AuthorObjectId>()
                           .MatchExpression("{$and:[{$gt:['$Age2','$Age']},{$eq:['$Surname','" + guid + "']}]}")
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(res.Surname, guid);
    }

    [TestMethod]
    public async Task find_with_aggregation_expression_using_template_works()
    {
        var guid = Guid.NewGuid().ToString();
        var author = new AuthorObjectId { Name = "a", Age = 10, Age2 = 11, Surname = guid };
        await author.SaveAsync();

        var template = new Template<AuthorObjectId>("{$and:[{$gt:['$<Age2>','$<Age>']},{$eq:['$<Surname>','<ObjectId>']}]}")
                       .Path(a => a.Age2)
                       .Path(a => a.Age)
                       .Path(a => a.Surname)
                       .Tag("ObjectId", guid);

        var res = (await DB.Find<AuthorObjectId>()
                           .MatchExpression(template)
                           .ExecuteAsync())
            .Single();

        Assert.AreEqual(res.Surname, guid);
    }

    [TestMethod]
    public async Task find_fluent_with_aggregation_expression_works()
    {
        var guid = Guid.NewGuid().ToString();
        var author = new AuthorObjectId { Name = "a", Age = 10, Age2 = 11, Surname = guid };
        await author.SaveAsync();

        var res = await DB.Fluent<AuthorObjectId>()
                          .Match(a => a.Surname == guid)
                          .MatchExpression("{$gt:['$Age2','$Age']}")
                          .SingleAsync();

        Assert.AreEqual(res.Surname, guid);
    }

    [TestMethod]
    public async Task find_with_include_required_props()
    {
        var review = new ReviewObjectId
        {
            Stars = 5,         //req
            Reviewer = "test", //req
            Rating = 1
        };
        await review.SaveAsync();

        var res = await DB.Find<ReviewObjectId>()
                          .MatchID(review.Id)
                          .Project(r => new() { Rating = r.Rating })
                          .IncludeRequiredProps()
                          .ExecuteSingleAsync();

        Assert.AreEqual(5, res!.Stars);
        Assert.AreEqual("test", res.Reviewer);
        Assert.AreEqual(1, res.Rating);
    }

    [TestMethod]
    public async Task update_and_get_with_include_required_props()
    {
        var review = new ReviewObjectId
        {
            Stars = 5,         //req
            Reviewer = "test", //req
            Rating = 1
        };
        await review.SaveAsync();

        var res = await DB.UpdateAndGet<ReviewObjectId>()
                          .MatchID(review.Id)
                          .Modify(r => r.Rating, 10)
                          .Project(r => new() { Rating = r.Rating })
                          .IncludeRequiredProps()
                          .ExecuteAsync();

        Assert.AreEqual(5, res!.Stars);
        Assert.AreEqual("test", res.Reviewer);
        Assert.AreEqual(10, res.Rating);
    }

    [TestMethod]
    public async Task decimal_properties_work_correctly()
    {
        var guid = Guid.NewGuid().ToString();
        var book1 = new BookObjectId { Title = guid, Price = 100.123m };
        await book1.SaveAsync();
        var book2 = new BookObjectId { Title = guid, Price = 100.123m };
        await book2.SaveAsync();

        var res = DB.Queryable<BookObjectId>()
                    .Where(b => b.Title == guid)
                    .GroupBy(b => b.Title)
                    .Select(
                        g => new
                        {
                            Title = g.Key,
                            Sum = g.Sum(b => b.Price)
                        }).Single();

        Assert.AreEqual(book1.Price + book2.Price, res.Sum);
    }

    [TestMethod]
    public async Task ignore_if_defaults_convention_works()
    {
        var author = new AuthorObjectId
        {
            Name = "test"
        };
        await author.SaveAsync();

        var res = await DB.Find<AuthorObjectId>().OneAsync(author.ID);

        Assert.IsTrue(res!.Age == 0);
        Assert.IsTrue(res.Birthday == null);
    }

    [TestMethod]
    public async Task custom_id_generation_logic_works()
    {
        var customer = new CustomerWithCustomID();
        await customer.SaveAsync();

        var res = await DB.Find<CustomerWithCustomID>().OneAsync(customer.ID);

        Assert.AreEqual(res!.ID, customer.ID);
    }

    [TestMethod]
    public async Task custom_id_used_in_a_relationship()
    {
        var customer = new CustomerWithCustomID();
        await customer.SaveAsync();

        var book = new BookObjectId { Title = "ciuiar", Customer = customer.ToReference() };
        await book.SaveAsync();

        var res = await book.Customer.ToEntityAsync();
        Assert.AreEqual(res.ID, customer.ID);

        var cus = await DB.Queryable<BookObjectId>()
                          .Where(b => b.Customer.ID == customer.ID)
                          .Select(b => b.Customer)
                          .SingleOrDefaultAsync();
        Assert.AreEqual(cus.ID, customer.ID);
    }

    [TestMethod]
    public async Task custom_id_override_string()
    {
        var e = new CustomIDOverride();
        await DB.SaveAsync(e);
        await Task.Delay(100);

        var creationTime = new DateTime(long.Parse(e.ID));

        Assert.IsTrue(creationTime < DateTime.UtcNow);
    }

    [TestMethod]
    public async Task custom_id_override_objectid()
    {
        var x = new CustomIDOverride
        {
            ID = ObjectId.GenerateNewId().ToString()
        };
        await x.SaveAsync();

        Assert.IsTrue(ObjectId.TryParse(x.ID, out _));
    }

    [TestMethod]
    public async Task custom_id_duplicate_throws()
    {
        var one = new CustomIDDuplicate();
        var two = new CustomIDDuplicate();
        await Assert.ThrowsExceptionAsync<MongoBulkWriteException<CustomIDDuplicate>>(
            () =>
                new[] { one, two }.SaveAsync());
    }
}