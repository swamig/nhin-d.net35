﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHINDirect.Diagnostics;
using Xunit;
using Xunit.Extensions;

namespace NHINDirect.Tests.Diagnostics
{
    public class Test24
    {
        LogFileSettings m_settings;
        LogWriter m_writer;
        
        public Test24()
        {
            m_settings = new LogFileSettings();
            m_settings.SetDefaults();
            m_settings.FileChangeFrequency = 24;
            
            m_writer = m_settings.CreateWriter();
        }
                
        [Fact]
        public void TestFileChange()
        {
            DateTime baseTime = new DateTime(2010, 1, 1, 0, 0, 0);                
            for (int i = 0; i < 17; ++i)
            {
                DateTime time = baseTime.AddHours(3 * i);
                TestChange(time, ((i % 8) == 0));
            }        
        }

        void TestChange(DateTime time, bool expectChange)
        {
            string filePath = m_writer.CurrentFilePath;
            m_writer.EnsureWriter(time);

            if (expectChange)
            {
                Assert.NotEqual<string>(filePath, m_writer.CurrentFilePath);
            }
            else
            {
                Assert.Equal<string>(filePath, m_writer.CurrentFilePath);
            }
        }
        
        [Fact]        
        public void TestSettingsDefault()
        {
            LogFileSettings settings = new LogFileSettings();
            Assert.DoesNotThrow(() => settings.SetDefaults());
        }
    }
}
