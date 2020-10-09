using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manafont.Db.Model
{
    public class ManafontCharacter
    {
        [Key]
        public string Id { get; set; }

        public ManafontUser User { get; set; } = null!;

        public ManafontCharacter() {
            Id = Guid.NewGuid().ToString();
        }
    }
}