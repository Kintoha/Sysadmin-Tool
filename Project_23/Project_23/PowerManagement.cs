using System;
using System.Runtime.InteropServices;

namespace Project_23
{
    class PowerManagement
    {
        // Импортируем API функцию InitiateSystemShutdown.
        [DllImport("advapi32.dll", EntryPoint = "InitiateSystemShutdownEx")]
        static extern int InitiateSystemShutdown(string lpMachineName, string lpMessage, int dwTimeout, bool bForceAppsClosed, bool bRebootAfterShutdown);

        // Импортируем API функцию AdjustTokenPrivileges.
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall,
        ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        // Импортируем API функцию GetCurrentProcess.
        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        // Импортируем API функцию OpenProcessToken.
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr h, int acc, ref IntPtr phtok);

        // Импортируем API функцию LookupPrivilegeValue.
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        // Импортируем API функцию LockWorkStation.
        [DllImport("user32.dll", EntryPoint = "LockWorkStation")]
        static extern bool LockWorkStation();

        // Объявляем структуру TokPriv1Luid для работы с привилегиями.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokPriv1Luid
        {
            public int Count;
            public long Luid;
            public int Attr;
        }

        // Объявляем необходимые, для API функций, константые значения, согласно MSDN.
        internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const int TOKEN_QUERY = 0x00000008;
        internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        internal const string SE_SHUTDOWN_NAME = "SeShutdownPrivilege";
        
        // Функция SetPriv для повышения привилегий процесса.
        private static void SetPriv()
        {
            // Экземпляр структуры TokPriv1Luid.
            TokPriv1Luid tkp;

            IntPtr htok = IntPtr.Zero;
            
            // Открываем "интерфейс" доступа для своего процесса.
            if (OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref htok))
            {
                // Заполняем поля структуры.
                tkp.Count = 1;
                tkp.Attr = SE_PRIVILEGE_ENABLED;
                tkp.Luid = 0;

                // Получаем системный идентификатор необходимой нам привилегии.
                LookupPrivilegeValue(null, SE_SHUTDOWN_NAME, ref tkp.Luid);
                
                //Повышем привилигеию своему процессу.
                AdjustTokenPrivileges(htok, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero);
            }
        }

        // Публичный метод для перезагрузки/выключения компьютера.
        public static int halt(bool RSh, bool Force)
        {
            //Получаем привилегии.
            SetPriv();

            // Вызываем функцию InitiateSystemShutdown, передавая ей необходимые параметры.
            return InitiateSystemShutdown(null, null, 0, Force, RSh);
        }

        // Публичный метод для блокировки операционной системы.
        public static int Lock()
        {
            if (LockWorkStation())
                return 1;
            else
                return 0;
        }
    }
}