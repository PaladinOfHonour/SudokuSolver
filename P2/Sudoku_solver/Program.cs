
        static void Solve(int time_limit = 600000)
            // SOLVE LOGIC
            Output(time_limit);
        // 
        static void FC(int frontier, ushort domain)
        {
            ushort value = (ushort)(1 << (frontier - 1));
            var pointer = v_p[frontier];
            var row = rows_c[pointer / sqrN];
            if ((row | value) > 0)
                throw new Exception("CONSTRAINT");
            var column = columns_c[pointer % sqrN];
            if ((column | value) > 0)
                throw new Exception("CONSTRAINT");

            v_domains[pointer] = value;
            // vp_++;
        }

        /// <summary>
        static void Solve(int time_limit = 600000)
            // SOLVE LOGIC
            Output(time_limit);
        // 
        static void FC(int frontier, ushort domain)
        {
            ushort value = (ushort)(1 << (frontier - 1));
            var pointer = v_p[frontier];
            var row = rows_c[pointer / sqrN];
            if ((row | value) > 0)
                throw new Exception("CONSTRAINT");
            var column = columns_c[pointer % sqrN];
            if ((column | value) > 0)
                throw new Exception("CONSTRAINT");

            v_domains[pointer] = value;
            // vp_++;
        }

        /// <summary>
        static void Solve(int time_limit = 600000)
            // SOLVE LOGIC
            Output(time_limit);
        // 
        static void FC(int frontier, ushort domain)
        {
            ushort value = (ushort)(1 << (frontier - 1));
            var pointer = v_p[frontier];
            var row = rows_c[pointer / sqrN];
            if ((row | value) > 0)
                throw new Exception("CONSTRAINT");
            var column = columns_c[pointer % sqrN];
            if ((column | value) > 0)
                throw new Exception("CONSTRAINT");

            v_domains[pointer] = value;
            // vp_++;
        }

        /// <summary>