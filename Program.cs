using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


/*      TODO
 * 1. 
 * 2. recursion looks like working for now.
 * 3. add memory checks after actions with buffer
 * 
 * 
 * 
 */
namespace ravendbtask
{
    class Program
    {
        static void Main(string[] args)
        {
            Trie t = new Trie();
            bool kek;
            long value;
            List<String> list = new List<String>();
            list.Add("bRH64OD4ltMVpxvOf7AGdm2VY965");
            list.Add("z8l1mW");
            list.Add("JIs1GCpjd1XnDXq1juIf");
            list.Add("ArLzfXdsltMVpxvOrzm4WFVzVznc");
            list.Add("RbH01pm7VUsfM4D94CBA");
            list.Add("JUDLptIiDFyzZt9UFjdi");
            list.Add("3xI0UgvAVEb7f7IT3TG1");
            list.Add("AKgfDf1XEvyvPzRbKwul");
            list.Add("iylfElwxUkOELePBHdAS");
            list.Add("1CyPF5OZG0wV0vPbfaIK");
            list.Add("Q4QtEEypqEoS7jA1PbkU");
            list.Add("RV1AulmktVnqLLS9p0if");
            for (int i = 0; i < list.Count; i++)
            {
                t.TryWrite(list[i], (long)(i + 1));
            }
            Console.WriteLine(t.TrieItemsCount());
            for (int i = 10; i < list.Count; i++)
            {
                var result = t.Delete(list[i]);
                Console.WriteLine("delete   " + list[i] +"  "+ result);

            }
            Console.WriteLine(t.TrieItemsCount());

            //Console.WriteLine("delete kex  " + kek);








            //t.TryWrite("a", 19);
            //t.TryWrite("kek", 155);
            //t.TryWrite("kep", 13);
            //t.TryWrite("kex", 10);
            //t.TryWrite("kexe", 12);
            //t.TryWrite("ke", 23);
            ////kek = t.Delete("kex");
            ////Console.WriteLine("delete kex  " + kek);
            ////kek = t.Delete("kexe");
            ////Console.WriteLine("delete kexe  " + kek);
            ////kek = t.Delete("a");
            ////Console.WriteLine("delete a  " + kek);
            //kek = t.Delete("ke");
            //Console.WriteLine("delete ke  " + kek);
            //kek = t.Delete("kek");
            //Console.WriteLine("delete kek  " + kek);
            //kek = t.Delete("ke");
            //Console.WriteLine("delete ke  " + kek);

            //kek = t.TryRead("kek", out value);
            //Console.WriteLine("read kek  " + value + "   " + kek);
            //kek = t.TryRead("kep", out value);
            //Console.WriteLine("read kep  " + value + "   " + kek);
            //kek = t.TryRead("kex", out value);
            //Console.WriteLine("read kex  " + value + "   " + kek);

            //kek = t.TryRead("kexe", out value);
            //Console.WriteLine("read kexe  " + value + "   " + kek);

            //kek = t.TryRead("a", out value);
            //Console.WriteLine("read a  " + value + "   " + kek);
            //kek = t.TryRead("ke", out value);
            //Console.WriteLine("read ke  " + value + "   " + kek);

            //kek = t.TryRead("kek", out value);
            //Console.WriteLine("read kek  " + value + "   " + kek);

            //kek = t.TryRead("ake", out value);
            //Console.WriteLine("read ke  " + value + "   " + kek);

            //kek = t.Delete("kep");
            //Console.WriteLine("delete kep  " + kek);
            //t.TryWrite("kek", 222);
            //kek = t.TryRead("kek", out value);
            //Console.WriteLine("read kek  " + value + "   " + kek);
            //kek = t.TryRead("kek", out value);
            //Console.WriteLine("read kek  " + value + "   " + kek);
        }
    }
    public interface ITrie
    {
        // Try to add the key/val to the trie
        // can fail if there isn't enough room
        // if already exists, will overwrite old 
        // value
        bool TryWrite(string key, long value);

        // Try to find the key in the trie, if found,
        // will put the value in the out param.
        // Can fail if value is not there
        bool TryRead(string key, out long value);

        // Remove the key from the trie, noop
        // if the key isn't there
        bool Delete(string key);

        // Saves the internal array to a file
        void Save(string filename);

        // Loads the internal array from a file
        void Load(string filename);
    }

    public class Trie : ITrie
    {
        private const int BUFFER_SIZE = 32 * 1024; // 32 KB
        private const short TRIE_HEADER_SIZE = 8;
        private byte[] buffer = new byte[BUFFER_SIZE];
        TrieInfo trie_header = new TrieInfo();

        // buffer helper structs
        private struct TrieInfo
        {
            // short - 2bytes, int - 4 bytes
            public short next_allocated_location;
            public short trie_used_space;
            public short trie_items_count;
            public short unused;
        }
        private struct NodeInfo
        {
            public short key_location;
            public short key_size;
            public short children_location;
            public short value_location;
        }

        // default constructor
        public Trie()
        {
            this.trie_header.next_allocated_location = TRIE_HEADER_SIZE;
            this.trie_header.trie_used_space = TRIE_HEADER_SIZE;
            this.trie_header.trie_items_count = 0;
            StructureToBuffer(this.trie_header, 0);
        }

        public bool TryWrite(string key, long value)
        {
            if (key.Length > BUFFER_SIZE || key.Length == 0 || value < 1) // mb make buffer-value and etc
            {
                // cant write data that are bigger than array size
                return false;
            }
            // read trie_header from buffer
            this.trie_header = BufferToTrieInfo();

            // new trie
            if (this.trie_header.trie_items_count == 0)
            {
                // create root_node
                // no key AND no value
                // just children offset = next_allocated_location
                NodeInfo root_node = new NodeInfo();
                int root_node_size = Marshal.SizeOf(root_node);
                root_node.key_location = 0;
                root_node.key_size = 0;
                root_node.children_location = (short)(this.trie_header.next_allocated_location + root_node_size);
                root_node.value_location = 0;
                ShortToBuffer(1, root_node.children_location);
                short childUsedSpace = getChildUsedSpaceFromBuffer(root_node.children_location);
                ShortToBuffer((short)(root_node.children_location + childUsedSpace), (root_node.children_location + sizeof(short)));

                this.trie_header.next_allocated_location = (short)(root_node.children_location + childUsedSpace);
                this.trie_header.trie_used_space = (short)(root_node.children_location + childUsedSpace);

                StructureToBuffer(this.trie_header, 0);
                StructureToBuffer(root_node, TRIE_HEADER_SIZE);


                InsertNodesToTrie(key, value, 0);
                return true;
            }
            if (this.trie_header.trie_items_count > 0)
            {
                // check first char of the given key for a match in root_node children
                int ifValue = CheckNodeChildrenForMatch(key.Substring(0, 1), TRIE_HEADER_SIZE);
                if (ifValue == 0)
                {
                    // new key first char isnt in trie => create new nodes
                    NodeInfo root_node = BufferToNodeInfo(TRIE_HEADER_SIZE);
                    //TrieInfo trie_head = BufferToTrieInfo();
                    // get root_node children array, resize and add next children location
                    byte[] arr = ResizeChildrenArray(buffer.Skip(root_node.children_location).Take(getChildUsedSpaceFromBuffer(root_node.children_location)).ToArray(), this.trie_header.next_allocated_location, "expand");
                    // copy to next_allocated_location
                    //buffer.CopyTo(arr, this.trie_header.next_allocated_location);
                    // set root_node children_location pointer to next_allocated_location


                    root_node.children_location = this.trie_header.next_allocated_location;
                    // update next_allocated_location
                    this.trie_header.next_allocated_location = (short)(this.trie_header.next_allocated_location + arr.Length);
                    this.trie_header.trie_used_space += sizeof(short);
                    
                    //update buffer
                    arr.CopyTo(buffer, root_node.children_location);    // add children array
                    StructureToBuffer(this.trie_header, 0);
                    StructureToBuffer(root_node, TRIE_HEADER_SIZE);
                    // insert key to trie
                    InsertNodesToTrie(key, value, 0);
                    return true;
                }
                else
                {
                    // new key first char is in trie => modify node
                    var result = checkMatch(ifValue, key, 1);
                    short nodeLocation = (short)result.Item1;
                    short keyPosition = (short)result.Item2;
                    // if keyPosition < key.Lenght => add new nodes
                    if (keyPosition < key.Length)
                    {
                        NodeInfo node = BufferToNodeInfo(nodeLocation);
                        // create new children array
                        // create new nodes 
                        // get node children array, resize and add next children location
                        byte[] arr;
                        if (node.children_location == 0)                // node got no children
                        {
                            arr = ResizeChildrenArray(null, this.trie_header.next_allocated_location, "new");
                        }
                        else
                        {
                            arr = ResizeChildrenArray(buffer.Skip(node.children_location).Take(getChildUsedSpaceFromBuffer(node.children_location)).ToArray(), this.trie_header.next_allocated_location, "expand");
                        }
                        // set root_node children_location pointer to next_allocated_location
                        node.children_location = this.trie_header.next_allocated_location;
                        // update next_allocated_location
                        this.trie_header.next_allocated_location = (short)(this.trie_header.next_allocated_location + arr.Length);
                        // update trie_used_space
                        this.trie_header.trie_used_space += sizeof(short);

                        //update buffer
                        arr.CopyTo(buffer, node.children_location);    // add children array
                        StructureToBuffer(node, nodeLocation);
                        StructureToBuffer(this.trie_header, 0);
                        InsertNodesToTrie(key, value, keyPosition);
                        return true;

                    }
                    else if (keyPosition == key.Length)       // if keyPosition == key.Lenght => just update val
                    {
                        // if given key is arleady in trie => update value
                        NodeInfo node = BufferToNodeInfo(nodeLocation);
                        if (node.value_location == 0)
                        {
                            // allocate new place for value
                            node.value_location = this.trie_header.next_allocated_location;
                            ValueToBuffer(value, node.value_location);

                            // update next_allocated_location
                            this.trie_header.next_allocated_location = (short)(this.trie_header.next_allocated_location + sizeof(long));
                            // update trie_used_space
                            this.trie_header.trie_used_space += sizeof(long);

                            // increase items counter
                            this.trie_header.trie_items_count++;

                            // write data to buffer
                            StructureToBuffer(node, nodeLocation);
                            StructureToBuffer(this.trie_header, 0);

                        }
                        else
                        {
                            ValueToBuffer(value, node.value_location);
                        }
                        // inrcrease item counter
                        return true;
                    }
                    else { } // delete later
                }
            }
            return false;
        }


        public bool TryRead(string key, out long value)
        {
            // set value to -1 (or 0) dunno yet
            value = -10;
            if (key.Length == 0)
            {
                return false;
            }
            this.trie_header = BufferToTrieInfo();
            // if trie is empty return false
            if (this.trie_header.trie_items_count == 0)
            {
                return false;
            }
            else {
                // check root_node children for key first char match
                // check first char of the given key for a match in root_node children
                int ifValue = CheckNodeChildrenForMatch(key.Substring(0, 1), TRIE_HEADER_SIZE);
                // root_node doesnt contain the first char of the given key => no match || no children (cannot happen)
                if (ifValue == 0 || ifValue == -1)
                {
                    return false;
                } else {
                    var result = checkMatch(ifValue, key, 1);
                    short nodeLocation = (short)result.Item1;
                    short keyPosition = (short)result.Item2;
                    // key and found key are the same length
                    if (keyPosition == key.Length)
                    {
                        // return value
                        NodeInfo node = BufferToNodeInfo(nodeLocation);
                        // if found key dont have value_location => found key is part of a key
                        if(node.value_location != 0)
                        {
                            value = ToShort(this.buffer[node.value_location], this.buffer[node.value_location + 1]);
                            return true;
                        } else {
                            return false;
                        }
                    } else {
                        return false;
                    }
                }
            }
        }
        public bool Delete(string key)
        {
            if (key.Length > BUFFER_SIZE || key.Length == 0)
            {
                // do nothing?
                return false;
            }
            else
            {
                // check first char of the given key for a match in root_node children
                int ifValue = CheckNodeChildrenForMatch(key.Substring(0, 1), TRIE_HEADER_SIZE);
                // there no match or root_node has no children
                if (ifValue == 0 || ifValue == -1)
                {
                    return false;
                }// there is a match in root_children and key string first character => 
                 // ifValue ==  key first character node location
                else
                {
                    //bool multiChild = false;
                    short deleteNodeLocation = 0;
                    for (int i = 0; i <= key.Length; i++)
                    {
                        string keyPart = key.Substring(0, key.Length - i);
                        // if keysubstring == ""
                        var result = checkMatch(ifValue, keyPart, 1);
                        short nodeLocation = (short)result.Item1;
                        short keyPosition = (short)result.Item2;

                        if(keyPosition == keyPart.Length)
                        {
                            // check children
                            NodeInfo node = BufferToNodeInfo(nodeLocation);
                            short childrenNumber = 0;
                            this.trie_header = BufferToTrieInfo();
                            if (node.children_location != 0)
                            {
                                childrenNumber = ToShort(this.buffer[node.children_location], this.buffer[node.children_location + 1]);
                            }
                            // the given key string is in the trie but doesnt have a value => not a valid key
                            if (node.value_location == 0 && i == 0)
                            {
                                return false;
                            }
                            else if (childrenNumber == 0 && node.value_location != 0 && i==0)
                            {
                                // remove complete node AND remove from upLevel node children array
                                deleteNodeLocation = nodeLocation;
                            }
                            else if (deleteNodeLocation == 0 && childrenNumber != 0 && node.value_location != 0 && i == 0)
                            {
                                
                                node.value_location = 0;
                                this.trie_header = BufferToTrieInfo();
                                this.trie_header.trie_used_space -= sizeof(long);
                                this.trie_header.trie_items_count--;
                                StructureToBuffer(this.trie_header, 0);
                                StructureToBuffer(node, nodeLocation);

                                return true;
                            } else if (deleteNodeLocation != 0 && childrenNumber == 1 && node.value_location == 0)
                            {
                                short deleteNodeSize = SizeOfNode(deleteNodeLocation);
                                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                                    - getChildUsedSpaceFromBuffer(node.children_location)
                                    - deleteNodeSize);
                                // create nodeUsedSpace method
                                node.children_location = 0;
                                StructureToBuffer(node, nodeLocation);
                                StructureToBuffer(this.trie_header, 0);
                                //StructureToBuffer(node, nodeLocation);
                                deleteNodeLocation = nodeLocation;
                            }

                            else if (deleteNodeLocation != 0 && childrenNumber == 1 && node.value_location != 0)
                            {
                                byte[] arr = ReduceChildrenArray(buffer.Skip(node.children_location).Take(getChildUsedSpaceFromBuffer(node.children_location)).ToArray(), deleteNodeLocation);
                                arr.CopyTo(buffer, node.children_location);
                                short deleteNodeSize = SizeOfNode(deleteNodeLocation);
                                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                                    - sizeof(short)
                                    - deleteNodeSize);
                                // delete one child no need to go through all nodes
                                this.trie_header = BufferToTrieInfo();
                                this.trie_header.trie_items_count--;
                                StructureToBuffer(this.trie_header, 0);
                                return true;
                            }


                            else if (deleteNodeLocation != 0 && childrenNumber > 1)
                            {
                                byte[] arr = ReduceChildrenArray(buffer.Skip(node.children_location).Take(getChildUsedSpaceFromBuffer(node.children_location)).ToArray(), deleteNodeLocation);
                                arr.CopyTo(buffer, node.children_location);
                                short deleteNodeSize = SizeOfNode(deleteNodeLocation);
                                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                                    - sizeof(short)
                                    - deleteNodeSize);
                                // delete one child no need to go through all nodes
                                this.trie_header = BufferToTrieInfo();
                                this.trie_header.trie_items_count--;
                                StructureToBuffer(this.trie_header, 0);
                                return true;
                            }

                            else
                            {
                                throw new Exception("Delete Failed");
                                //return false;
                            }

                        } else if (keyPosition < keyPart.Length) // given key is bigger than value in trie
                        {
                            return false;
                        }
                        // root node
                        else
                        {
                            // given key first character node => delete from root node
                            NodeInfo root_node = BufferToNodeInfo(TRIE_HEADER_SIZE);
                            this.trie_header = BufferToTrieInfo();
                            short childrenNumber = ToShort(this.buffer[root_node.children_location], this.buffer[root_node.children_location + 1]);
                            // root has more children
                            if (childrenNumber > 1)
                            {
                                NodeInfo node = BufferToNodeInfo(deleteNodeLocation);
                                string foundKey = BufferToString(node.key_location, node.key_size);
                                Console.WriteLine("deleteNodeLocation  foundKey    " + foundKey + "  ");


                                byte[] arr = ReduceChildrenArray(buffer.Skip(root_node.children_location).Take(getChildUsedSpaceFromBuffer(root_node.children_location)).ToArray(), deleteNodeLocation);
                                arr.CopyTo(buffer, root_node.children_location);
                                short deleteNodeSize = SizeOfNode(deleteNodeLocation);
                                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                                    - sizeof(short)
                                    - deleteNodeSize);
                                this.trie_header = BufferToTrieInfo();
                                this.trie_header.trie_items_count--;
                                StructureToBuffer(this.trie_header, 0);
                                return true;
                            }
                            // root has no children => delete children location
                            else if(childrenNumber == 1)
                            {
                                // modify current node children_location to zero => delete this node child (deleteNodeLocation)
                                // remove current node children size from used_space
                                // remove delete node size from used_space
                                //short nodeSize = SizeOfNode(nodeLocation);
                                short deleteNodeSize = SizeOfNode(deleteNodeLocation);
                                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                                    - getChildUsedSpaceFromBuffer(root_node.children_location)
                                    - deleteNodeSize);
                                // create nodeUsedSpace method
                                root_node.children_location = 0;
                                StructureToBuffer(root_node, TRIE_HEADER_SIZE);
                                this.trie_header = BufferToTrieInfo();
                                this.trie_header.trie_items_count--;
                                StructureToBuffer(this.trie_header, 0);
                                return true;
                                //StructureToBuffer(this.trie_header, 0);
                            }
                        }
                    }

                    //this.trie_header = BufferToTrieInfo();
                    //this.trie_header.trie_items_count--;
                    //StructureToBuffer(this.trie_header, 0);
                    //return true;
                    return false;
                }
            }
        }

        public void Load(string filename)
        {
            throw new NotImplementedException();
        }

        public void Save(string filename)
        {
            throw new NotImplementedException();
        }



        public short TrieItemsCount()
        {
            this.trie_header = BufferToTrieInfo();
            return this.trie_header.trie_items_count;
        }


        private short SizeOfNode(short nodeLocation)
        {
            NodeInfo node = BufferToNodeInfo(nodeLocation);
            short alignedKeySize = 0;
            short childrenArraySize = 0;
            short valueSize = 0;
            if (node.key_size != 0)
            {
                alignedKeySize = (short)(node.key_size - node.key_size % 8 + 8);
            }
            if(node.children_location != 0)
            {
                childrenArraySize = getChildUsedSpaceFromBuffer(node.children_location);
            }
            if(node.value_location != 0)
            {
                valueSize = sizeof(long);
            }
            return (short)(8 + alignedKeySize + childrenArraySize + valueSize);
        }


        private void InsertNodesToTrie(String key, long value, int keyPosition)
        {
            int keyLength = key.Length;
            for (int i = keyPosition; i < keyLength; i++)
            {
                //char currentCharacter = key[i];
                string currentSubstring = key.Substring(0, i + 1); // need 2 test
                short currentKeyLength = (short)currentSubstring.Length;

                if (i < keyLength - 1)
                {
                    // add new node
                    // with 1 children
                    // empty value (-1 ?)
                    // currentSubstring is the key
                    // ++trie_items_count
                    // update next_allocated_location, trie_used_space, trie_items_count
                    NodeInfo node = new NodeInfo();
                    node.key_location = (short)(this.trie_header.next_allocated_location + (short)(Marshal.SizeOf(node)));
                    node.key_size = currentKeyLength;
                    short alignedKeySize = (short)(currentKeyLength - (currentKeyLength % 8) + 8);
                    node.value_location = 0;
                    //node.value_location = (short)(alignedKeySize + node.key_location);
                    //node.children_location = (short)(node.value_location + sizeof(long));
                    node.children_location = (short)(alignedKeySize + node.key_location);
                    StructureToBuffer(node, this.trie_header.next_allocated_location);
                    StringToBuffer(currentSubstring, node.key_location);
                    //ValueToBuffer(0, node.value_location);
                    ShortToBuffer(1, node.children_location);
                    ShortToBuffer((short)(node.children_location + getChildUsedSpaceFromBuffer(node.children_location)), (node.children_location + sizeof(short)));

                    this.trie_header.next_allocated_location = (short)(node.children_location + getChildUsedSpaceFromBuffer(node.children_location));
                    this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                        + (short)(Marshal.SizeOf(node))
                        + alignedKeySize
                        + getChildUsedSpaceFromBuffer(node.children_location));

                    StructureToBuffer(this.trie_header, 0);

                }
                else if (i == keyLength - 1)
                {
                    // add new node
                    // without children
                    // with value
                    // currentSubstring is the key
                    // ++trie_items_count
                    NodeInfo node = new NodeInfo();
                    node.key_location = (short)(this.trie_header.next_allocated_location + (short)(Marshal.SizeOf(node)));
                    node.key_size = currentKeyLength;
                    short alignedKeySize = (short)(currentKeyLength - (currentKeyLength % 8) + 8);
                    //node.value_location = 0;
                    node.value_location = (short)(alignedKeySize + node.key_location);
                    //node.children_location = (short)(node.value_location + sizeof(long));
                    node.children_location = 0;
                    //node.children_location = (short)(alignedKeySize + node.key_location);
                    StructureToBuffer(node, this.trie_header.next_allocated_location);
                    StringToBuffer(currentSubstring, node.key_location);
                    ValueToBuffer(value, node.value_location);
                    //ShortToBuffer(1, node.children_location);
                    //ShortToBuffer((short)(node.children_location + getChildUsedSpaceFromBuffer(node.children_location)), (node.children_location + sizeof(short)));
                    this.trie_header.trie_items_count += 1;
                    this.trie_header.next_allocated_location = (short)(node.value_location + sizeof(long));
                    this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space
                        + (short)(Marshal.SizeOf(node))
                        + alignedKeySize
                        + sizeof(long));

                    StructureToBuffer(this.trie_header, 0);
                }
            }
        }

        Tuple<int, int> checkMatch(int nodeLocation, string key, int keyPosition)
        {
            if (key.Length > keyPosition)
            {
                int nextNodeLocation = CheckNodeChildrenForMatch(key.Substring(0, keyPosition + 1), nodeLocation);
                if(nextNodeLocation > 0)                // theres is a match in children
                {
                    return checkMatch(nextNodeLocation, key, keyPosition + 1); // continue recursion
                } else if (nextNodeLocation == 0)      // no match in children => add children nodes with remaining key part 
                {
                    //return Tuple.Create(nodeLocation, keyPosition);
                    return Tuple.Create(nodeLocation, keyPosition);
                }
                else                                  // node has no children => add children nodes with remaining key part
                {
                    //return Tuple.Create(nextNodeLocation, keyPosition + 1);
                    return Tuple.Create(nodeLocation, keyPosition);
                }
            }
            else if (key.Length == keyPosition)
            {
                // Key is in the trie => update value
                return Tuple.Create(nodeLocation, keyPosition);
            }
            else
            {
                return Tuple.Create(nodeLocation, keyPosition);
            }
        }

        int CheckNodeChildrenForMatch(string keyFirstChar, int nodeLocation)
        {

            NodeInfo root_node = BufferToNodeInfo(nodeLocation);
            if(root_node.children_location == 0)
            {
                return -1;
            }
            int childrenNumber = ToShort(buffer[root_node.children_location], buffer[root_node.children_location + 1]);
            //int firstChildLocation = ToShort(buffer[root_node.children_location + sizeof(short)], buffer[root_node.children_location + sizeof(short) + 1]);
            if (childrenNumber > 0)
            {
                for (int i = 0; i < childrenNumber; i++)
                {
                    int ChildLocation = ToShort(buffer[root_node.children_location + sizeof(short) + i * sizeof(short)], buffer[root_node.children_location + sizeof(short) + i * sizeof(short) + 1]);
                    NodeInfo node = BufferToNodeInfo(ChildLocation);
                    string foundKey = BufferToString(node.key_location, node.key_size);
                    if (foundKey.Equals(keyFirstChar))
                    {
                        // return found node location
                        return node.key_location - Marshal.SizeOf(node);
                    }
                }
                // return zero if no match in children
                return 0;
            }
            else
            {
                // return -1 if no children
                return -1;
            }
        }

        byte[] ReduceChildrenArray(byte[] arr, short deleteNodeLocation)
        {
            // temp array
            byte[] temp = new byte[arr.Length - sizeof(short)];
            byte byte1, byte2;
            FromShort(deleteNodeLocation, out byte1, out byte2);
            Console.WriteLine("deleteNodeLocation    " + deleteNodeLocation + "  ");
            int k = 0;
            for (int i = 0; i < arr.Length; i = i + 2)
            {
                Console.WriteLine("node_locations    " + ToShort(arr[i], arr[i + 1]) + "  ");
                if(deleteNodeLocation == ToShort(arr[i], arr[i + 1]))
                {
                    k = i;
                }
                //ToShort(arr[i], arr[i + 1]);
            }

            NodeInfo nodeq = BufferToNodeInfo(deleteNodeLocation);
            string foundKeyz = BufferToString(nodeq.key_location, nodeq.key_size);
            Console.WriteLine("deleteNodeLocation  foundKey    " + foundKeyz + "  ");
            Console.WriteLine("arr.Length  size    " + arr.Length + "  ");
            Console.WriteLine("temp  size    " + temp.Length + "  ");

            int p = Array.IndexOf(arr, byte1);
            Console.WriteLine("p    " + p + "   K   "+ k);

            int j = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (i != k && i != k + 1)
                {
                    temp[j] = arr[i];
                    j++;
                }

            }
            for(int i=0; i< arr.Length; i = i + 2)
            {
                NodeInfo node = BufferToNodeInfo(ToShort(arr[i], arr[i + 1]));
                string foundKey = BufferToString(node.key_location, node.key_size);
                Console.WriteLine("arr  foundKey    " + foundKey + "  ");

            }
            for (int i = 0; i < temp.Length; i = i + 2)
            {
                NodeInfo node = BufferToNodeInfo(ToShort(temp[i], temp[i + 1]));
                string foundKey = BufferToString(node.key_location, node.key_size);
                Console.WriteLine("temp  foundKey    " + foundKey + "  ");
            }

            FromShort((short)(ToShort(arr[0], arr[1]) - 1), out byte1, out byte2);
            temp[0] = byte1; temp[1] = byte2;
            return temp;
        }



        byte[] ResizeChildrenArray(byte[] arr, short newChildArrayLocation, string str)
        {
            if (str == "new")
            {
                // create new array with size of two shorts
                arr = new byte[2 * sizeof(short)];
                byte byte1, byte2;
                FromShort(1, out byte1, out byte2);
                arr[0] = byte1; arr[1] = byte2;
                FromShort((short)(newChildArrayLocation + arr.Length), out byte1, out byte2);
                arr[arr.Length - 2] = byte1; arr[arr.Length - 1] = byte2;
                return arr;
            }
            else if (str == "expand")
            {
                Array.Resize(ref arr, arr.Length + sizeof(short));
                byte byte1, byte2;
                FromShort((short)(ToShort(arr[0], arr[1]) + 1), out byte1, out byte2);
                arr[0] = byte1; arr[1] = byte2;
                FromShort((short)(newChildArrayLocation + arr.Length), out byte1, out byte2);
                arr[arr.Length - 2] = byte1; arr[arr.Length - 1] = byte2;
                return arr;
            } else
            {
                return arr;
            }
            //} else if (str == "reduce")
            //{
            //    return arr;
            //}
        }

        public static void FromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number >> 0);
        }

        public static short ToShort(byte byte1, byte byte2)
        {
            return (short)((byte2 << 8) | (byte1 << 0));
        }

        void StringToBuffer(string str, int location)
        {
            short alignedKeySize = (short)(str.Length - (str.Length % 8) + 8);
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            int k = 0;
            for (int j = location; j <= (location + alignedKeySize); j++)
            {
                if (k < bytes.Length)
                {
                    this.buffer[j] = bytes[k];
                    k++;
                }
            }
        }

        string BufferToString(int keyLocation, short keySize)
        {
            short alignedKeySize = (short)(keySize - (keySize % 8) + 8);
            string str = Encoding.UTF8.GetString(buffer.Skip(keyLocation).Take(keySize).ToArray());
            return str;
        }
        void ValueToBuffer(long num, int location)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            //Console.WriteLine("bytes.Length " + bytes.Length);
            int k = 0;
            for (int j = location; j < (location + sizeof(long)); j++)
            {
                if (k < bytes.Length)
                {
                    this.buffer[j] = bytes[k];
                    k++;
                }
            }
        }

        void ShortToBuffer(short num, int location)
        {
            byte byte1, byte2;
            FromShort(num, out byte1, out byte2);
            buffer[location] = byte1;
            buffer[location + 1] = byte2;
        }

        short getChildUsedSpaceFromBuffer(int location)
        {
            short children_number = ToShort(this.buffer[location], this.buffer[location + 1]);
            return (short)(sizeof(short) * children_number + sizeof(short));
        }

        void StructureToBuffer(object obj, int copyLocation)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);
            arr.CopyTo(buffer, copyLocation);
        }

        TrieInfo BufferToTrieInfo()
        {
            TrieInfo str = new TrieInfo();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(this.buffer, 0, ptr, size);

            str = (TrieInfo)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
        NodeInfo BufferToNodeInfo(int copyLocation)
        {
            NodeInfo str = new NodeInfo();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(this.buffer, copyLocation, ptr, size);

            str = (NodeInfo)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
    }
}