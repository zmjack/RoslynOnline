using Microsoft.EntityFrameworkCore;
using Northwnd;

namespace RoslynOnline;

public class NorthwndSqliteContext : NorthwndContext
{
    public NorthwndSqliteContext()
        //: this(new DbContextOptionsBuilder().UseSqlite("DataSource=local.db").Options)
        : this(new DbContextOptionsBuilder().UseSqlite("DataSource=file:worker-promiser.sqlite3?vfs=opfs").Options)
    {
    }

    public NorthwndSqliteContext(DbContextOptions options) : base(options)
    {
    }
}
