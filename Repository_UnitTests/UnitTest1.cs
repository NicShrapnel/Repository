using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityFramework_Repository;
using SimulationExternalProject_DAL;
using System.Linq;
using System.Collections.Generic;

namespace Repository_UnitTests
{
    [TestClass]
    public class Repository_UnitTests
    {
        [TestMethod]
        public void TestLoggingToDifferentConnectionString()
        {
            using (var pctx = new ProLawEntities())
            using (var ctx = new ProLawIntegrationEntities())
            {
                var r = new Repository(pctx,
                    EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection,
                    null,
                    false,
                    ctx.Database.Connection);
                r.QueueContextChange(new MattersQFORECLOSUR6
                {
                    MattersQFORECLOSUR61 = "RepoTest",
                    MattersQFORECLOSUREPRO = "RepoTest",
                    QTOTITLETYPE = "RepoTest"
                });
                r.CommitContextChanges();
                Assert.IsNotNull(pctx.MattersQFORECLOSUR6.Where(f => f.MattersQFORECLOSUREPRO == "RepoTest"));

                r.RollBackAllChanges();
            }
        }
        [TestMethod]
        public void TestRollbackQueueAll()
        {
            var r = new Repository();

            using (var ctx = new ProLawEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);

                r.QueueContextChange(new MattersQFORECLOSUR6
                {
                    MattersQFORECLOSUREPRO = "RepoTestCreationAndRollbackAll",
                    MattersQFORECLOSUR61 = "RepoTestCreationAndRollbackAll",
                    AddingDateTime = DateTime.Now,
                    QTOTITLETYPE = "RepoTest"
                });
                r.RemoveAllFromQueue();

                r.CommitContextChanges();

                Assert.IsNull(ctx.MattersQFORECLOSUR6.Where(m => m.MattersQFORECLOSUR61 == "RepoTestCreationAndRollbackAll").FirstOrDefault());
            }
        }
        [TestMethod]
        public void TestRollbackQueueRange()
        {
            var r = new Repository();

            using (var ctx = new ProLawEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);

                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestCreationAndRollbackRange",
                    MatterID = "RepoTestCreationAndRollbackRange",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });
                r.RemoveRangeFromQueue(2,4);

                r.CommitContextChanges();

                Assert.IsNull(ctx.Matters.Where(m => m.MatterID == "RepoTestCreationAndRollbackRange").FirstOrDefault());
                r.RollBackAllChanges();
            }
        }
        [TestMethod]
        public void TestRollbackQueueSingle()
        {
            var r = new Repository();

            using (var ctx = new ProLawEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);

                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestCreationAndRollbackAll",
                    MatterID = "RepoTestCreationAndRollbackAll",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });
                r.RemoveFromQueueByID(2);

                r.CommitContextChanges();

                Assert.IsNull(ctx.Matters.Where(m => m.MatterID == "RepoTestCreationAndRollbackAll").FirstOrDefault());
                r.RollBackAllChanges();
            }
        }
        [TestMethod]
        public void TestCreationAndRollbackAll()
        {
            var r = new Repository();

            using (var ctx = new ProLawEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);

                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestCreationAndRollbackAll",
                    MatterID = "RepoTestCreationAndRollbackAll",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                foreach (var item in r.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                r.RemoveAllFromQueue();

                r.CommitContextChanges();

                Assert.IsNull(ctx.Matters.Where(m => m.Matters1 == "RepoTestCreationAndRollbackAll").FirstOrDefault());
            }
        }

        [TestMethod]
        public void TestCreationAndRollbackSingle()
        {
            var r = new Repository();
            long? selection = null;

            using (var ctx = new ProLawEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestCreationAndRollbackAll",
                    MatterID = "RepoTestCreationAndRollbackAll2",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsNotNull(ctx.Matters.Where(c => c.MatterID == "RepoTestCreationAndRollbackAll2").FirstOrDefault());

                foreach (var item in r.ChangeLogQueue.Where(c => c.CommittedAt.HasValue))
                {
                    if (item.NewValue == "RepoTestCreationAndRollbackAll2")
                        selection = item.Entry_ID;

                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

            }
            Console.Write("Enter a single ChangeLog Entry ID to roll back: ");
            Console.WriteLine(selection.ToString());

            Assert.IsTrue(r.RollBackChangeByID(selection.Value));

            using (var ctx = new ProLawEntities())
            {
                var matter = ctx.Matters.Where(c => c.MatterID == "RepoTestCreationAndRollbackAll2").FirstOrDefault();
                Assert.IsNull(matter);
            }
        }

        [TestMethod]
        public void TestCreationAndRollbackRange()
        {
            var r = new Repository();

            using (var ctx = new ProLawEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestUpdateAndRollbackRange",
                    MatterID = "RepoTestUpdateAndRollbackRange",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                long minValue = 0;
                long maxValue = 0;
                foreach (var item in r.ChangeLogQueue.ToList())
                {
                    if (item == r.ChangeLogQueue.ToList()[0])
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
                Assert.IsNull(ctx.Matters.Where(m => m.Matters1 == "RepoTestUpdateAndRollbackRange").FirstOrDefault());
            }
        }
        [TestMethod]
        public void TestUpdateAndRollbackAll()
        {
            Repository r = null;

            using (var ctx = new ProLawEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestUpdateAndRollbackAll",
                    MatterID = "RepoTestUpdateAndRollbackAll",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                Assert.IsTrue(r.CommitContextChanges());

                var toUpdate = ctx.Matters.Where(m => m.Matters1 == "TestUpdateAndRollbackAll").FirstOrDefault();
                Assert.IsNotNull(toUpdate);

                toUpdate.MatterID = null;
                r.QueueContextChange(toUpdate);

                foreach (var item in r.ChangeLogQueue.Where(q => q.CommittedAt == null))
                {
                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsTrue(r.RollBackAllChanges());
                Assert.IsNull(ctx.Matters.Where(m => m.MatterID == "TestUpdateAndRollbackAll" || m.MatterID == null).FirstOrDefault());
            }
        }

        [TestMethod]
        public void TestUpdateAndRollbackSingle()
        {
            Repository r = null;

            using (var ctx = new ProLawEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);
                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestUpdateAndRollbackSingl",
                    MatterID = "RepoTestUpdateAndRollbackSingl",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                Assert.IsTrue(r.CommitContextChanges());

                var toUpdate = ctx.Matters.Where(m => m.Matters1 == "RepoTestUpdateAndRollbackSingl").FirstOrDefault();
                Assert.IsNotNull(toUpdate);

                toUpdate.MatterID = null;
                toUpdate.AddingDateTime = null;
                toUpdate.AddingProfessionals = null;

                r.QueueContextChange(toUpdate);

                foreach (var item in r.ChangeLogQueue.Where(q => q.CommittedAt == null))
                {
                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                var changeList = r.ChangeLogQueue.Where(q => q.PrimaryKey == toUpdate.Matters1.ToString()).ToList();
                var changeID = changeList.Where(change => change.ColumnUpdated == "MatterID").Select(change => change.Entry_ID).FirstOrDefault();

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsNotNull(ctx.Matters.Where(m => m.MatterID == null).FirstOrDefault());
                Assert.IsTrue(r.RollBackChangeByID(changeID));
                Assert.IsNull(ctx.Matters.Where(m => m.MatterID == null).FirstOrDefault());
            }
        }
        [TestMethod]
        public void TestUpdateAndRollbackRange()
        {
            Repository r = null;

            using (var ctx = new ProLawEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);
                r.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestUpdateAndRollbackRange",
                    MatterID = "RepoTestUpdateAndRollbackRange",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                Assert.IsTrue(r.CommitContextChanges());

                var toUpdate = ctx.Matters.Where(m => m.Matters1 == "RepoTestUpdateAndRollbackRange").FirstOrDefault();
                Assert.IsNotNull(toUpdate);

                toUpdate.MatterID = null;
                toUpdate.AddingDateTime = null;
                toUpdate.AddingProfessionals = null;

                r.QueueContextChange(toUpdate);

                foreach (var item in r.ChangeLogQueue.Where(q => q.CommittedAt == null))
                {
                    Console.WriteLine(item.Entry_ID + ") " + item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                var idList = r.ChangeLogQueue.Where(q => q.PrimaryKey == toUpdate.Matters1.ToString()
                && (q.ColumnUpdated == "MatterID" || q.ColumnUpdated == "AddingProfessionals")).Select(q => q.Entry_ID);

                Assert.AreNotEqual(0, idList.Count());
                CollectionAssert.AllItemsAreUnique(idList.ToList());

                var begChangeID = idList.OrderBy(id => id).FirstOrDefault();
                var endChangeID = idList.OrderByDescending(id => id).FirstOrDefault();

                Assert.IsTrue(begChangeID < endChangeID);

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsTrue(r.RollBackChangesByRange(begChangeID, endChangeID));
                Assert.IsNull(ctx.Matters.Where(m => m.MatterID == null).FirstOrDefault());
            }
        }

        [TestMethod]
        public void TestRollingBackSingleFromPreviousIteration()
        {
            using (var ctx = new ProLawEntities())
            {
                var PreviousIterationRepo = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);
                PreviousIterationRepo.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestPrevCreation,RollbackSingle",
                    MatterID = "RepoTestPrevCreation,RollbackSingle",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });


                foreach (var item in PreviousIterationRepo.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(PreviousIterationRepo.CommitContextChanges());
                Assert.IsNotNull(ctx.Matters.Where(m => m.Matters1 == "RepoTestPrevCreation,RollbackSingle").FirstOrDefault());
            }

            Repository r = null;
            long changeID;
            using (var ctx = new ProLawEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                var matter = ctx.Matters.Where(m => m.Matters1 == "RepoTestPrevCreation,RollbackSingle").OrderByDescending(m => m.Matters1).FirstOrDefault();

                Assert.IsNotNull(matter);

                changeID = ctx.RepositoryChangeLogs.Where(q => q.PrimaryKey == matter.Matters1)
                    .Where(c => c.ColumnUpdated == "MatterID")
                    .Select(c => c.RepositoryChangeLogId)
                    .FirstOrDefault();

                Assert.IsTrue(r.CommitContextChanges());
                Assert.IsNotNull(ctx.Matters.Where(m => m.MatterID == "RepoTestPrevCreation,RollbackSingle").FirstOrDefault());
            }

            Assert.IsTrue(r.RollBackChange_Made_By_Previous_Repo_ByID(changeID));

            using (var ctx = new ProLawEntities())
            {
                Assert.IsNull(ctx.Matters.Where(m => m.MatterID == "RepoTestPrevCreation,RollbackSingle").FirstOrDefault());
            }
        }
        [TestMethod]
        public void TestRollingBackRangeFromPreviousIteration()
        {
            using (var ctx = new ProLawEntities())
            {
                var PreviousIterationRepo = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);
                PreviousIterationRepo.QueueContextChange(new Matters
                {
                    Matters1 = "RepoTestPrevCreation,RollbackRange",
                    MatterID = "RepoTestPrevCreation,RollbackRange",
                    AddingDateTime = DateTime.Now,
                    AddingProfessionals = "RepoTest"
                });

                foreach (var item in PreviousIterationRepo.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(PreviousIterationRepo.CommitContextChanges());
                Assert.IsNotNull(ctx.Matters.Where(m => m.Matters1 == "RepoTestPrevCreation,RollbackRange").FirstOrDefault());
            }

            Repository r = null;
            List<long> changeIDList;
            using (var ctx = new ProLawEntities())
            {
                r = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

                var matter = ctx.Matters.Where(m => m.Matters1 == "RepoTestPrevCreation,RollbackRange").FirstOrDefault();

                changeIDList = ctx.RepositoryChangeLogs.Where(q => q.PrimaryKey == matter.Matters1).Select(q => q.RepositoryChangeLogId).ToList();
            }

            Assert.IsTrue(r.RollBackChanges_Made_By_Previous_Repo_ByRange(changeIDList.First(), changeIDList.Last()));

            using (var ctx = new ProLawEntities())
            {
                Assert.IsNull(ctx.Matters.Where(m => m.Matters1 == "RepoTestPrevCreation,RollbackRange").FirstOrDefault());
            }

        }
        [TestMethod]
        public void TestGetAllChangesForEntity()
        {
            MattersQFORECLOSUR6 f6 = null;
            using (var ctx = new ProLawEntities())
            {
                var PreviousIterationRepo = new Repository(ctx, EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);
                f6 = new MattersQFORECLOSUR6
                {
                    MattersQFORECLOSUREPRO = "RepoTestGetAllChangesForEntity",
                    MattersQFORECLOSUR61 = "RepoTestGetAllChangesForEntity",
                    AddingDateTime = DateTime.Now,
                    QTOTITLETYPE = "RepoTest"
                };

                PreviousIterationRepo.QueueContextChange(f6);

                foreach (var item in PreviousIterationRepo.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                Assert.IsTrue(PreviousIterationRepo.CommitContextChanges());
                Assert.IsNotNull(ctx.MattersQFORECLOSUR6.Where(m => m.MattersQFORECLOSUR61 == "RepoTestGetAllChangesForEntity").FirstOrDefault());
            }

            Repository r = null;
            
            r = new Repository(new ProLawEntities(), EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection, null, false);

            var allChanges = r.GetAllChanges_ThisEntity(f6).ToList();
            var recentChanges = r.ChangeLogQueue.ToList();

            Assert.AreEqual(0, recentChanges.Count);
            Assert.AreNotEqual(allChanges.Count, recentChanges.Count);
            Assert.IsTrue(r.RollBackChanges_Made_By_Previous_Repo_ByRange(allChanges[0].Entry_ID, allChanges[allChanges.Count - 1].Entry_ID));

            using (var ctx = new ProLawEntities())
            {
                Assert.IsNull(ctx.Matters.Where(m => m.Matters1 == "RepoTestGetAllChangesForEntity").FirstOrDefault());
            }
        }
    }
}
