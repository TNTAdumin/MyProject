using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyProject.Domain.Models
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 添加日期
        /// DateTime.UtcNow表示当前的协调世界时间（UTC）的属性，在处理跨时区的情况时，使用 DateTime.UtcNow 可以避免时区转换带来的问题，因为它返回的是一个标准的时间。
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// 修改日期
        /// </summary>
        public DateTime UpdatedDate { get; set; }
        /// <summary>
        /// 是否已删除
        /// </summary>
        public bool IsDeleted { get; set; } = false; // 软删除标志
    }
}
