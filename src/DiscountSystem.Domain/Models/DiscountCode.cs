using System;
using System.Collections.Generic;
using System.Text;

namespace DiscountSystem.Domain.Models
{
    public class DiscountCode
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }

        // Safe concurrency control, especially in systems with frequent updates
        public byte[] RowVersion { get; set; } = default!;

    }
}
