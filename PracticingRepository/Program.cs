using EntityFramework_Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticingRepository
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Repository();

            using (var ctx = new CraftsmanEntities())
            {
                r.LoadContext(ctx);
                r.LoadLogger(EntityFramework_Repository.ConnectionFactory.ConnectionMethod.CurrentContextConnection);

                r.QueueContextChange(new Cards
                {
                    CardName = "Blargh",
                    Effect = "Honk"
                });

                foreach (var item in r.ChangeLogQueue)
                {
                    Console.WriteLine(item.TableUpdated + "." + item.ColumnUpdated + ": " + item.NewValue);
                }

                if (r.CommitContextChanges())
                    r.RollBackAllChanges();

                Console.ReadKey();
            }
        }
    }
}
