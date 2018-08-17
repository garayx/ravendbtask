using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ravendbtask;
using System.Collections.Generic;



/*
 *ctr+r, t          to run
 *
 * */
namespace ravendbtaskTester
{
    [TestClass]
    public class TrieTests
    {
        //[TestMethod]
        //public void Write_Empty_StringTo_Trie()
        //{
        //    var trie = new Trie();
        //    bool result;
        //}


        [TestMethod]
        public void Write_To_Empty_Trie()
        {
            var trie = new Trie();
            //short intResult;
            bool result;
            // write to trie
            result = trie.TryWrite("hi", 100);
            Assert.AreEqual(true, result);
        }
        [TestMethod]
        public void Write_Empty_String_To_Trie()
        {
            var trie = new Trie();
            bool result;
            result = trie.TryWrite("", 99);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void Write_Empty_Negative_Value_To_Trie()
        {
            var trie = new Trie();
            bool result;
            // write negative value to trie
            result = trie.TryWrite("negativeValue", -22);
            Assert.AreEqual(false, result);
        }
        [TestMethod]
        public void Read_From_Empty_Trie()
        {
            var trie = new Trie();
            bool result;
            long val;
            // try to read an empty trie
            result = trie.TryRead("hi", out val);
            Assert.AreEqual(false, result);
        }
        [TestMethod]
        public void Read_Added_Key_From_Trie()
        {
            var trie = new Trie();
            bool result;
            long val;
            result = trie.TryWrite("hi", 100);
            Assert.AreEqual(true, result);
            result = trie.TryRead("hi", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(100, val);
        }
        [TestMethod]
        public void Read_Empty_Key_From_Trie()
        {
            var trie = new Trie();
            bool result;
            long val;
            result = trie.TryWrite("hi", 100);
            Assert.AreEqual(true, result);
            result = trie.TryRead("", out val);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void Read_Non_Existing_Key_From_Trie()
        {
            var trie = new Trie();
            long val;
            bool result;
            // try to read a non existing key
            result = trie.TryWrite("hi", 100);
            Assert.AreEqual(true, result);
            result = trie.TryRead("ih", out val);
            Assert.AreEqual(false, result);
        }
        [TestMethod]
        public void Overwrite_Existing_Value()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            result = trie.TryWrite("hi", 100);
            Assert.AreEqual(true, result);
            result = trie.TryRead("hi", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(100, val);
            result = trie.TryWrite("hi", 322);
            Assert.AreEqual(true, result);
            result = trie.TryRead("hi", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            num = trie.TrieItemsCount();
            Assert.AreEqual(1, num);
        }
        [TestMethod]
        public void Write_Item_That_Starts_With_Previous()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            result = trie.TryWrite("kek e", 228);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek e", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(228, val);
            num = trie.TrieItemsCount();
            Assert.AreEqual(2, num);
        }
        [TestMethod]
        public void Write_Item_That_Starts_With_Same_Prefix()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            result = trie.TryWrite("kep", 228);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kep", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(228, val);
            num = trie.TrieItemsCount();
            Assert.AreEqual(2, num);
        }
        [TestMethod]
        public void Write_Item_That_Is_A_Prefix_Of_Existing_One()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            result = trie.TryWrite("ke", 228);
            Assert.AreEqual(true, result);
            result = trie.TryRead("ke", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(228, val);
            num = trie.TrieItemsCount();
            Assert.AreEqual(2, num);
        }
        [TestMethod]
        public void Write_Items_That_Start_With_Different_Chars()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            result = trie.TryWrite("ake", 228);
            Assert.AreEqual(true, result);
            result = trie.TryRead("ake", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(228, val);
            result = trie.TryWrite("eke", 997);
            Assert.AreEqual(true, result);
            result = trie.TryRead("eke", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(997, val);
            num = trie.TrieItemsCount();
            Assert.AreEqual(3, num);
        }
        [TestMethod]
        public void Add_Many_Strings()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            List<String> list = new List<String>();
            list.Add("TkZCVTFmZi35xTjAUQue");
            list.Add("UuhB8osFNeg11sa47auI");
            list.Add("FJrQmnwN78oeCgdE3MBK");
            list.Add("cpqmA6lsb0QtLsMkyP6tzayegrCM");
            list.Add("1cs9F8PX4ZC12ZZsuIAs");
            list.Add("7jTBkuC1u6NgqvgcmUyO");
            list.Add("u98cyz1bj0JNEAyYCs7H");
            list.Add("yBeeYYXIZ3LRbgj5EJ2m");
            list.Add("8JJFCp2KkCdNiWRSL6F6");
            list.Add("u7PlMxx0");
            list.Add("Vjlsb0QtLsTkwKGjiPFh");
            list.Add("Jvlkr5fQ6aN5m4fy30cX");
            list.Add("5sEtpTuUZpp6uqk7Pk5M");
            list.Add("xuPY8fato4Z29Z9Igpdh");
            list.Add("FditIKxWbKQLU3b7PbiB");
            list.Add("55aQGL0zTSLeI92lH5Yd");
            list.Add("3f1a7rN4tDreXdchY2kH");
            list.Add("MSvZP7wkT99pHFCk9pGz");
            list.Add("u7PlMxxhz9XRIBzQ6Co0");
            list.Add("bRH64OD4ltMVpxvOf7AGdm2VY965");
            list.Add("RC2MR9BTGkFV3Nxggo9K");
            list.Add("z8l1mW");
            list.Add("JIs1GCpjd1XnDXq1juIf");
            list.Add("qRk92ChLdY5AIctORtS5");
            list.Add("ArLzfXdsltMVpxvOrzm4WFVzVznc");
            list.Add("RbH01pm7VUsfM4D94CBA");
            list.Add("sXZscoyG3sKKEwKHyFhW");
            list.Add("FykQ812gyStE6rUW6sK2");
            list.Add("e5OMRwAXa3rNuS9wX2kK");
            list.Add("JUDLptIiDFyzZt9UFjdi");
            list.Add("3xI0UgvAVEb7f7IT3TG1");
            list.Add("AKgfDf1XEvyvPzRbKwul");
            list.Add("iylfElwxUkOELePBHdAS");
            list.Add("TkZCVTFjAUQue");
            list.Add("Hbz9WvX0KTRP40eYoXIF");
            list.Add("tl8hyC85oYyGybHxvuG7");
            list.Add("72SAAbag3kR9JBjX5aW4");
            list.Add("WxNZCh191xUZ0pH8yTPg");
            list.Add("1CyPF5OZG0wV0vPbfaIK");
            list.Add("NOd2QHltMVpxvOnhQtjLWMXaQxS2");
            list.Add("RRjYsCAEvW5MzERGYdLw");
            list.Add("G9FbQb1fZfLZ2SVq7QEl");
            list.Add("uMjQTllt5tlauPhn0KNu");
            list.Add("Aq53hm9OlSXovEz8PvZK");
            list.Add("Q4QtEEypqEoS7jA1PbkU");
            list.Add("1YQmTrFHfWlrH3B0AOxY");
            list.Add("6sZTxAMQcMtDuFbkxnZr");
            list.Add("nc7rMXAgqhMDfSs3sP0M");
            list.Add("XpPQUTXygn9VwTAC2djH");
            list.Add("RV1AulmktVnqLLS9p0if");
            list.Add("Zzy1kOXcOVcTs6bxKFdc");
            list.Add("2eXWyjbDxtKhze3ZReNX");
            list.Add("2eXWyjbDxtKhz2eXWyjbDxtKhe3ZReNX");
            //list.Add("ZZSq47oSEfVSa8ZbJyQu");
            //list.Add("zBcGVQ1O4eC0xsrhRFY2");
            //list.Add("xDdy7VGmhvqTtvU1Gz48");
            //list.Add("scokVQOA7M4qWluZloxy");
            //list.Add("AfkHt4R9ltMVpxvOv9MTaeJACqsy");
            //list.Add("B8ZYxQMBvHEiqIh9yDfN");
            //list.Add("vLP3ViKRIBPNk4JscLqS");
            //list.Add("I8ltMVpxvOClpY1MkXX9");
            //list.Add("2IsvdClxGrfVYjdxs7cq");
            //list.Add("2NMaco7An8eQr1f2Q7EO");
            //list.Add("K2FTCChAJqUmQRJNa3Rc");
            //list.Add("wDK3Qz5ec8FVgzlvK707");
            //list.Add("Eg6T97DBVdrKZguPOesa");
            //list.Add("EDJuPVJiHzOu6tW69U2W");
            //list.Add("LfEMkmeObhCg02SIMqMv");
            //list.Add("szI3Z0j7zGNug1CT3r8Y");
            //list.Add("0jQIgORCk8x7o9r3NpPL");
            //list.Add("8e5bAWsP5oec8FVgz5AcYKPWqCc");
            //list.Add("zzKrDN1qzjBaszluWxQS");
            //list.Add("P0Y2G3jFBBRSzfw5Iae0");
            //list.Add("RQo9GzW8xE5uaMdslndm");
            //list.Add("jCqnG5TJuWHHQySuQwJI");
            //list.Add("KfyN3LcDwRJWr0oqnfEe");
            //list.Add("Gqdues17JiTxf48r28gv");
            //list.Add("ze6Rqbj3wHYyyr0lRv9Y");
            //list.Add("skZQepO5XMFVOG9d8MiR");
            //list.Add("Qfm8PDu65ZmTxrMbER52");
            //list.Add("NSOpIwWzcTLQ6gsmIe5D");
            //list.Add("sAXM6HvTvTKkDqn25C7n");
            //list.Add("K8IolT0PSQhc0FaqP605");
            //list.Add("Scjk0iXOxTQTq9LiI7Q8");
            //list.Add("wDK3Qzec8FVgz5ec8FVgzlec8FVgzvK707");
            //list.Add("HFrOKVU22JsxhWk2Y0ND");
            //list.Add("qbj80DhzMYSca9EScSYU");
            //list.Add("XwyteBec8FVgzRTddu48KeIqNFU");
            //list.Add("zhSMcgpOa4OmIuryJcyq");
            //list.Add("BBifCPk1QCm2d5Ga0cXv");
            //list.Add("TDCnhORrStYbdZXL4TI8");
            //list.Add("UOO6xnpa3AwjIdKq7LlV");
            //list.Add("IvH7t7eIHaunbTY9wT90");
            //list.Add("Hqa4nD0EAVkKMPbQ3Rd9");
            //list.Add("zcTcAcqnwPva3QrYEDj2");
            //list.Add("igWTxBSuM4RCl0Bx7adc");
            //list.Add("GIlsD2aXE8Fi9WdqLJpS");
            //list.Add("vyXvTEIcZGpLpDQluV3J");
            //list.Add("dIXgQXucBvRgSsCfVtle");
            //list.Add("bZXVi94M2ttTEhEX8qoA");
            //list.Add("ZYwGpgG75vmJOfAdI1b8");
            //list.Add("HQLOXNQdUBe27agyUt5a");
            //list.Add("UR17guZ7JBqtCpNxIGFm");
            //list.Add("yuPRkYCrQ9hJsX1eHskj");
            for (int i = 0; i < list.Count; i++)
            {
                result = trie.TryWrite(list[i], (long)(i+1));
                Assert.AreEqual(true, result);
            }
            num = trie.TrieItemsCount();
            Assert.AreEqual(list.Count, num);
            for (int i = 0; i < list.Count; i++)
            {
                result = trie.TryRead(list[i], out val);
                Assert.AreEqual(true, result);
                Assert.AreEqual(i+1, val);
            }
        }
        [TestMethod]
        public void Can_Remove_Item()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            num = trie.TrieItemsCount();
            Assert.AreEqual(1, num);
            result = trie.Delete("kek");
            Assert.AreEqual(true, result);
            num = trie.TrieItemsCount();
            Assert.AreEqual(0, num);
        }
        [TestMethod]
        public void Can_Remove_Same_prefix_Item()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;

            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryWrite("ke", 228);
            Assert.AreEqual(true, result);

            num = trie.TrieItemsCount();
            Assert.AreEqual(2, num);

            result = trie.Delete("ke");
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            result = trie.TryRead("ke", out val);
            Assert.AreEqual(false, result);

            num = trie.TrieItemsCount();
            Assert.AreEqual(1, num);
        }
        [TestMethod]
        public void Can_Remove_Longer_Item()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;

            result = trie.TryWrite("kek", 322);
            Assert.AreEqual(true, result);
            result = trie.TryWrite("kekooooool", 228);
            Assert.AreEqual(true, result);

            num = trie.TrieItemsCount();
            Assert.AreEqual(2, num);

            result = trie.Delete("kekooooool");
            Assert.AreEqual(true, result);
            result = trie.TryRead("kek", out val);
            Assert.AreEqual(true, result);
            Assert.AreEqual(322, val);
            result = trie.TryRead("kekooooool", out val);
            Assert.AreEqual(false, result);

            num = trie.TrieItemsCount();
            Assert.AreEqual(1, num);
        }
        [TestMethod]
        public void Remove_Many_Strings()
        {
            var trie = new Trie();
            bool result;
            long val;
            short num;
            List<String> list = new List<String>();
            list.Add("TkZCVTFmZi35xTjAUQue");
            list.Add("UuhB8osFNeg11sa47auI");
            list.Add("FJrQmnwN78oeCgdE3MBK");
            list.Add("cpqmA6lsb0QtLsMkyP6tzayegrCM");
            list.Add("1cs9F8PX4ZC12ZZsuIAs");
            list.Add("7jTBkuC1u6NgqvgcmUyO");
            list.Add("u98cyz1bj0JNEAyYCs7H");
            list.Add("yBeeYYXIZ3LRbgj5EJ2m");
            list.Add("8JJFCp2KkCdNiWRSL6F6");
            list.Add("u7PlMxx0");
            list.Add("Vjlsb0QtLsTkwKGjiPFh");
            list.Add("Jvlkr5fQ6aN5m4fy30cX");
            list.Add("5sEtpTuUZpp6uqk7Pk5M");
            list.Add("xuPY8fato4Z29Z9Igpdh");
            list.Add("FditIKxWbKQLU3b7PbiB");
            list.Add("55aQGL0zTSLeI92lH5Yd");
            list.Add("3f1a7rN4tDreXdchY2kH");
            list.Add("MSvZP7wkT99pHFCk9pGz");
            list.Add("u7PlMxxhz9XRIBzQ6Co0");
            list.Add("bRH64OD4ltMVpxvOf7AGdm2VY965");
            list.Add("RC2MR9BTGkFV3Nxggo9K");
            list.Add("z8l1mW");
            list.Add("JIs1GCpjd1XnDXq1juIf");
            list.Add("qRk92ChLdY5AIctORtS5");
            list.Add("ArLzfXdsltMVpxvOrzm4WFVzVznc");
            list.Add("RbH01pm7VUsfM4D94CBA");
            list.Add("sXZscoyG3sKKEwKHyFhW");
            list.Add("FykQ812gyStE6rUW6sK2");
            list.Add("e5OMRwAXa3rNuS9wX2kK");
            list.Add("JUDLptIiDFyzZt9UFjdi");
            list.Add("3xI0UgvAVEb7f7IT3TG1");
            list.Add("AKgfDf1XEvyvPzRbKwul");
            list.Add("iylfElwxUkOELePBHdAS");
            list.Add("TkZCVTFjAUQue");
            list.Add("Hbz9WvX0KTRP40eYoXIF");
            list.Add("tl8hyC85oYyGybHxvuG7");
            list.Add("72SAAbag3kR9JBjX5aW4");
            list.Add("WxNZCh191xUZ0pH8yTPg");
            list.Add("1CyPF5OZG0wV0vPbfaIK");
            list.Add("NOd2QHltMVpxvOnhQtjLWMXaQxS2");
            list.Add("RRjYsCAEvW5MzERGYdLw");
            list.Add("G9FbQb1fZfLZ2SVq7QEl");
            list.Add("uMjQTllt5tlauPhn0KNu");
            list.Add("Aq53hm9OlSXovEz8PvZK");
            list.Add("Q4QtEEypqEoS7jA1PbkU");
            list.Add("1YQmTrFHfWlrH3B0AOxY");
            list.Add("6sZTxAMQcMtDuFbkxnZr");
            list.Add("nc7rMXAgqhMDfSs3sP0M");
            list.Add("XpPQUTXygn9VwTAC2djH");
            list.Add("RV1AulmktVnqLLS9p0if");
            list.Add("Zzy1kOXcOVcTs6bxKFdc");
            list.Add("2eXWyjbDxtKhze3ZReNX");
            list.Add("2eXWyjbDxtKhz2eXWyjbDxtKhe3ZReNX");
            for (int i = 0; i < list.Count; i++)
            {
                result = trie.TryWrite(list[i], (long)(i + 1));
                Assert.AreEqual(true, result);
            }
            num = trie.TrieItemsCount();
            Assert.AreEqual(list.Count, num);
            for (int i = 0; i < list.Count; i++)
            {
                result = trie.TryRead(list[i], out val);
                Assert.AreEqual(true, result);
                Assert.AreEqual(i + 1, val);
            }
            for (int i = 0; i < list.Count; i++)
            {
                result = trie.Delete(list[i]);
                Assert.AreEqual(true, result);
            }
            num = trie.TrieItemsCount();
            Assert.AreEqual(0, num);
        }


    }
}
