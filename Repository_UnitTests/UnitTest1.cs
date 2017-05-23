using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityFramework_Repository;
using PracticingRepository;
using System.Linq;
using System.Collections.Generic;

namespace Repository_UnitTests
{
    [TestClass]
    public class Repository_UnitTests
    {
        [TestMethod]
        public void TestCreationAndRollbackAll()
        {
            var r = new Repository();

            using (var ctx = new CraftsmanEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);

                r.QueueContextChange(new Cards
                {
                    CardName = "TestCreationAndRollbackAll",
                    Effect = "Honk"
                });

                foreach (var item in r.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsTrue(r.RollBackAllChanges());
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestCreationAndRollbackAll").FirstOrDefault());
            }
        }

        [TestMethod]
        public void TestCreationAndRollbackSingle()
        {
            var r = new Repository();
            long? selection = null;

            using (var ctx = new CraftsmanEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Cards
                {
                    CardName = "TestCreationAndRollbackSingle",
                    Effect = "Honk"
                });

                Assert.IsTrue(r.CommitContextChanges());

                foreach (var item in r.ChangeLogQueue.Where(c => c.CommittedAt.HasValue))
                {
                    if (item.NewValue == "Honk")
                        selection = item.Entry_ID;

                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

            }
            Console.Write("Enter a single ChangeLog Entry ID to roll back: ");
            Console.WriteLine(selection.ToString());

            Assert.IsTrue(r.RollBackChangeByID(selection.Value));

            using (var ctx = new CraftsmanEntities())
            {
                var card = ctx.Cards.Where(c => c.CardName == "TestCreationAndRollbackSingle" && c.Effect == "Honk").FirstOrDefault();
                Assert.IsNull(card);
            }
        }
        [TestMethod]
        public void TestCreationAndRollbackRange()
        {
            var r = new Repository();

            using (var ctx = new CraftsmanEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Cards
                {
                    CardName = "TestCreationAndRollbackRange",
                    Effect = "Honk"
                });

                long minValue = 0;
                long maxValue = 0;
                foreach (var item in r.ChangeLogQueue)
                {
                    if (item == r.ChangeLogQueue[0])
                    {
                        minValue = item.Entry_ID;
                        maxValue = item.Entry_ID;
                    }

                    if (item.Entry_ID < minValue)
                        minValue = item.Entry_ID;
                    if (item.Entry_ID > maxValue)
                        maxValue = item.Entry_ID;

                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(r.CommitContextChanges());
                Console.Write("Enter a range of ChangeLog Entry IDs to roll back: ");

                Assert.IsTrue(r.RollBackChangesByRange(minValue, maxValue));
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestCreationAndRollbackRange").FirstOrDefault());
            }
        }
        [TestMethod]
        public void TestUpdateAndRollbackAll()
        {
            Repository r = null;

            using (var ctx = new CraftsmanEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Cards
                {
                    CardName = "TestUpdateAndRollbackAll",
                    IsCovered = true,
                    IsWork = true,
                    Script = "TestUpdateAndRollbackAll",
                    Effect = "TestUpdateAndRollbackAll"
                });
                Assert.IsTrue(r.CommitContextChanges());

                var toUpdate = ctx.Cards.Where(c => c.CardName == "TestUpdateAndRollbackAll").FirstOrDefault();
                Assert.IsNotNull(toUpdate);

                toUpdate.CardName = "Blargh";
                r.QueueContextChange(toUpdate);

                foreach (var item in r.ChangeLogQueue.Where(q => q.CommittedAt == null))
                {
                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsTrue(r.RollBackAllChanges());
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestUpdateAndRollbackAll").FirstOrDefault());
            }
        }

        [TestMethod]
        public void TestUpdateAndRollbackSingle()
        {
            Repository r = null;

            using (var ctx = new CraftsmanEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Cards
                {
                    CardName = "TestUpdateAndRollbackSingle",
                    Effect = "Honk"
                });
                Assert.IsTrue(r.CommitContextChanges());

                var toUpdate = ctx.Cards.Where(c => c.CardName == "TestUpdateAndRollbackSingle").FirstOrDefault();
                Assert.IsNotNull(toUpdate);

                foreach (var item in r.ChangeLogQueue.Where(q => q.CommittedAt == null))
                {
                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                var changeIDList = r.ChangeLogQueue.Where(q => q.PrimaryKey == toUpdate.CardID.ToString()).Select(q => q.Entry_ID).ToList();
                var changeID = changeIDList.OrderBy(c => c).Skip(1).FirstOrDefault();
                
                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsTrue(r.RollBackChangeByID(changeID));
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestUpdateAndRollbackSingle").FirstOrDefault());
            }
        }
        [TestMethod]
        public void TestUpdateAndRollbackRange()
        {
            Repository r = null;

            using (var ctx = new CraftsmanEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Cards
                {
                    CardName = "Blargh",
                    Effect = "Honk"
                });
                Assert.IsTrue(r.CommitContextChanges());

                var toUpdate = ctx.Cards.Where(c => c.CardName == "Blargh").FirstOrDefault();
                Assert.IsNotNull(toUpdate);

                toUpdate.CardName = "TestUpdateAndRollbackRange";
                toUpdate.IsCovered = true;
                toUpdate.IsWork = true;
                toUpdate.Script = "TestUpdateAndRollbackRange";
                toUpdate.Effect = "TestUpdateAndRollbackRange";

                r.QueueContextChange(toUpdate);

                foreach (var item in r.ChangeLogQueue.Where(q => q.CommittedAt == null))
                {
                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                var idList = r.ChangeLogQueue.Where(q => q.PrimaryKey == toUpdate.CardID.ToString() 
                && (q.ColumnUpdated == "CardName" || q.ColumnUpdated == "Effect")).Select(q => q.Entry_ID);

                Assert.AreNotEqual(0, idList.Count());
                CollectionAssert.AllItemsAreUnique(idList.ToList());
                
                var begChangeID = idList.OrderBy(id => id).FirstOrDefault();
                var endChangeID = idList.OrderByDescending(id => id).FirstOrDefault();

                Assert.IsTrue(begChangeID < endChangeID);

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsTrue(r.RollBackChangesByRange(begChangeID, endChangeID));
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestUpdateAndRollbackRange").FirstOrDefault());
                Assert.IsNull(ctx.Cards.Where(c => c.Effect == "TestUpdateAndRollbackRange").FirstOrDefault());
            }
        }

        [TestMethod]
        public void TestRollingBackSingleFromPreviousIteration()
        {
            using (var ctx = new CraftsmanEntities())
            {
                var PreviousIterationRepo = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);
                PreviousIterationRepo.QueueContextChange(new Cards
                {
                    CardName = "TestRollingBackSingleFromPreviousIteration",
                    Effect = "Honk"
                });

                foreach (var item in PreviousIterationRepo.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(PreviousIterationRepo.CommitContextChanges());
                Assert.IsNotNull(ctx.Cards.Where(c => c.CardName == "TestRollingBackSingleFromPreviousIteration").FirstOrDefault());
            }

            Repository r = null;
            List<long> changeIDList;
            long? changeID = null;
            using (var ctx = new CraftsmanEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                var card = ctx.Cards.Where(c => c.CardName == "TestRollingBackSingleFromPreviousIteration").OrderByDescending(c => c.CardID).FirstOrDefault();

                Assert.IsNotNull(card);

                //can't change primary keys
                changeIDList = ctx.RepositoryChangeLogs.Where(q => q.PrimaryKey == card.CardID.ToString()).Select(q => q.RepositoryChangeLogId).ToList();

                changeID = changeIDList.OrderBy(c => c).Skip(1).FirstOrDefault();
            }

            Assert.IsTrue(r.CommitContextChanges());
            Assert.IsTrue(r.RollBackChange_Made_By_Previous_Repo_ByID(changeID.Value));

            using (var ctx = new CraftsmanEntities())
            {
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestRollingBackSingleFromPreviousIteration").FirstOrDefault());
            }

        }
        [TestMethod]
        public void TestRollingBackRangeFromPreviousIteration()
        {
            using (var ctx = new CraftsmanEntities())
            {
                var PreviousIterationRepo = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);
                PreviousIterationRepo.QueueContextChange(new Cards
                {
                    CardName = "TestRollingBackRangeFromPreviousIteration",
                    Effect = "Honk"
                });

                foreach (var item in PreviousIterationRepo.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(PreviousIterationRepo.CommitContextChanges());
                Assert.IsNotNull(ctx.Cards.Where(c => c.CardName == "TestRollingBackRangeFromPreviousIteration").FirstOrDefault());
            }

            Repository r = null;
            List<long> changeIDList;
            using (var ctx = new CraftsmanEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                var card = ctx.Cards.Where(c => c.CardName == "TestRollingBackRangeFromPreviousIteration").FirstOrDefault();

                changeIDList = ctx.RepositoryChangeLogs.Where(q => q.PrimaryKey == card.CardID.ToString()).Select(q => q.RepositoryChangeLogId).ToList();
            }
            
            Assert.IsTrue(r.RollBackChanges_Made_By_Previous_Repo_ByRange(changeIDList.First(), changeIDList.Last()));

            using (var ctx = new CraftsmanEntities())
            {
                Assert.IsNull(ctx.Cards.Where(c => c.CardName == "TestRollingBackRangeFromPreviousIteration").FirstOrDefault());
            }

        }
    }
}
