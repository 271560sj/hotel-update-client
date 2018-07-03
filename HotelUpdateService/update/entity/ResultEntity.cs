﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotelUpdateService.update.entity
{
    //用于记录版本校验的返回结果
    class ResultEntity
    {
        //返回结果的消息
        public String message { get; set; }

        //返回结果的code
        public int code { get; set; }

        //返回的路径信息
        public String path { get; set; }

        public String hash { get; set; }

    }
}
