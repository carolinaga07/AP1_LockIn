using Microsoft.EntityFrameworkCore;

namespace Lock_In.Context
{
    public class Contexto : DbContext
    {
        public Contexto(DbContextOptions<Contexto>options): base(options) { }

    }
}
