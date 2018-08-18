using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ravendbtask
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
    /// <summary>
    /// The Trie interface.
    /// Contains all methods that I had to create.
    /// </summary>
    public interface ITrie
    {
        /// <summary>
        /// Try to add the key/val to the trie
        /// can fail if there isn't enough room
        /// if already exists, will overwrite old 
        /// value.
        /// Only positive values accepted
        /// </summary>
        /// <returns>
        /// true if write operation was succesfull
        /// false if write operation failed
        /// </returns>
        bool TryWrite(string key, long value);

        /// <summary>
        /// Try to find the key in the trie, if found,
        /// will put the value in the out param.
        /// Can fail if value is not there
        /// </summary>
        /// <returns>
        /// true and a long value if read operation was succesfull
        /// false and -10 if read operation failed
        /// </returns>
        bool TryRead(string key, out long value);
        /// <summary>
        /// Remove the key from the trie, noop
        /// if the key isn't there
        /// </summary>
        /// <returns>
        /// true if delete operation was succesfull
        /// false if delete operation failed
        /// </returns>
        bool Delete(string key);

        /// <summary>
        /// Saves the internal array to a file
        /// </summary>
        void Save(string filename);

        /// <summary>
        /// Loads the internal array from a file
        /// </summary>
        void Load(string filename);
    }
    /// <summary>
    /// The main Trie class
    /// Contains all methods to preform Trie operations and their help methods
    /// </summary>
    /// <remarks>
    /// This class can add, read, delete node from trie.
    /// </remarks>
    public class Trie : ITrie
    {
        private const int BUFFER_SIZE = 32 * 1024; // 32 KB
        private const short TRIE_HEADER_SIZE = 8;
        private byte[] buffer = new byte[BUFFER_SIZE];
        TrieInfo trie_header = new TrieInfo();

        /// <summary>
        /// Trie Header struct, contains infromation about the trie widely used to save and load trie information to buffer
        /// </summary>
        private struct TrieInfo
        {
            // short - 2bytes, int - 4 bytes
            // I made them public so I dont have to write set/get methods
            public short next_allocated_location;
            public short trie_used_space;
            public short trie_items_count;
            public short unused;                // must be kept
        }

        /// <summary>
        /// Trie Node struct, contains information about trie nodes.
        /// </summary>
        private struct NodeInfo
        {
            public short key_location;
            public short key_size;
            public short children_location;
            public short value_location;
        }

        /// <summary>
        /// default constructor, sets initial trie_header values and write them to buffer
        /// </summary>
        public Trie()
        {
            this.trie_header.next_allocated_location = TRIE_HEADER_SIZE;
            this.trie_header.trie_used_space = TRIE_HEADER_SIZE;
            this.trie_header.trie_items_count = 0;
            StructureToBuffer(this.trie_header, 0);
        }
        /// <summary>
        /// TryWrite method implementation
        /// </summary>
        public bool TryWrite(string key, long value)
        {
            // calculate the projected size of key in buffer
            int projectedSizeInBuffer = sizeof(long); ;
            for(int i=0; i < key.Length; i++)
            {
                projectedSizeInBuffer = projectedSizeInBuffer + 8 + 2 * sizeof(short) + i - i % 8 + 8;
            }
            // the projected size is smaller then buffer avaible space
            if(projectedSizeInBuffer < GetAvaibleSpace())
            {
                return InsertNodes(key, value);
            } 
            // the projected size is bigger the buffer avaible space
            else
            {
                // defrag buffer
                DefragBuffer();
                if (projectedSizeInBuffer < GetAvaibleSpace())
                {
                    return InsertNodes(key, value);
                } else
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// TryRead method implementation
        /// </summary>
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
        /// <summary>
        /// Delete method implementation
        /// </summary>
        public bool Delete(string key)
        {
            if (key.Length > BUFFER_SIZE || key.Length == 0)
            {
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
                    short deleteNodeLocation = 0;
                    for (int i = 0; i <= key.Length; i++)
                    {
                        string keyPart = key.Substring(0, key.Length - i);
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
                            }
                        }
                    }
                    return false;
                }
            }
        }
        /// <summary>
        /// Load method implementation
        /// </summary>
        public void Load(string filename)
        {
            if (filename.Length > 0)
            {
                byte[] temp;
                temp = File.ReadAllBytes(filename);
                if (temp.Length != BUFFER_SIZE)
                {
                    throw new Exception("wrong file size");
                }
                else
                {
                    Array.Copy(temp, this.buffer, BUFFER_SIZE);
                }
            }
        }
        /// <summary>
        /// Save method implementation
        /// </summary>
        public void Save(string filename)
        {
            if(filename.Length > 0)
            {
                File.WriteAllBytes(filename, this.buffer);
            }
        }
        /// <summary>
        /// InsertNodes method implementation,  this is a helper method of <see cref="TryWrite(string, long)"/> which does all the magic!
        /// </summary>
        private bool InsertNodes(string key, long value)
        {
            if (key.Length > BUFFER_SIZE || key.Length == 0 || value < 1)
            {
                // cant write data that are bigger than array size
                return false;
            }
            // read trie_header from buffer
            this.trie_header = BufferToTrieInfo();

            // new trie
            if (this.trie_header.trie_items_count == 0)
            {
                // just children offset = next_allocated_location
                // create root_node
                NodeInfo root_node = new NodeInfo();
                int root_node_size = Marshal.SizeOf(root_node);
                // set up the values of root_node
                root_node.key_location = 0;
                root_node.key_size = 0;
                root_node.children_location = (short)(this.trie_header.next_allocated_location + root_node_size);
                root_node.value_location = 0;
                ShortToBuffer(1, root_node.children_location);
                short childUsedSpace = getChildUsedSpaceFromBuffer(root_node.children_location);
                ShortToBuffer((short)(root_node.children_location + childUsedSpace), (root_node.children_location + sizeof(short)));
                // update used space and allocate location
                this.trie_header.next_allocated_location = (short)(root_node.children_location + childUsedSpace);
                this.trie_header.trie_used_space = (short)(root_node.children_location + childUsedSpace);
                // write to buffer
                StructureToBuffer(this.trie_header, 0);
                StructureToBuffer(root_node, TRIE_HEADER_SIZE);

                // inser the given key to trie
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
                    else { } // wont get here
                }
            }
            return false;
    }
        /// <summary>
        /// TrieItemsCount method implementation, returns the current number of items in trie
        /// </summary>
        public short TrieItemsCount()
            {
                this.trie_header = BufferToTrieInfo();
                return this.trie_header.trie_items_count;
            }
        /// <summary>
        /// DefragBuffer method implementation, the method writes all trie values and nodes from the start of byte array to save space
        /// </summary>
        private bool DefragBuffer()
        {
            TrieInfo trieheader = BufferToTrieInfo();
            NodeInfo root_node = BufferToNodeInfo(TRIE_HEADER_SIZE);

            if (trieheader.trie_items_count > 0) {
                byte[] tempBuffer = new byte[BUFFER_SIZE];
                //Array.Copy(this.buffer, tempBuffer, this.buffer.Length);
                //this.buffer = new byte[BUFFER_SIZE];

                this.trie_header.next_allocated_location = 2 * TRIE_HEADER_SIZE;
                this.trie_header.trie_used_space = 2 * TRIE_HEADER_SIZE;
                this.trie_header.trie_items_count = trieheader.trie_items_count;

                byte[] root_node_children_array = this.buffer.Skip(root_node.children_location).Take(getChildUsedSpaceFromBuffer(root_node.children_location)).ToArray();
                root_node.children_location = this.trie_header.next_allocated_location;
                // write root_node children array to temp buffer
                //Array.Copy(root_node_children_array,
                //    0, 
                //    tempBuffer,
                //    this.trie_header.next_allocated_location,
                //    root_node_children_array.Length);

                //save space for root node children array
                this.trie_header.next_allocated_location = (short)(this.trie_header.next_allocated_location + root_node_children_array.Length);
                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space + root_node_children_array.Length);
                // write trie_header AND trie_root_node to tempBuffer
                StructureToBuffer(this.trie_header, 0, ref tempBuffer);
                StructureToBuffer(root_node, TRIE_HEADER_SIZE, ref tempBuffer);
                for (int i = 2; i < root_node_children_array.Length; i = i + 2)
                {
                    short childNodeLocation = ToShort(root_node_children_array[i], root_node_children_array[i + 1]);
                    
                    // change child location to next_allocated_location
                    this.trie_header = BufferToTrieInfo(ref tempBuffer);
                    byte byte1, byte2;
                    FromShort(this.trie_header.next_allocated_location, out byte1, out byte2);
                    root_node_children_array[i] = byte1;
                    root_node_children_array[i + 1] = byte2;


                    DefragBufferHelpFunction(ref tempBuffer, childNodeLocation);
                }
                // write root_node children array to temp buffer
                Array.Copy(root_node_children_array,
                0,
                tempBuffer,
                16,
                root_node_children_array.Length);

                this.buffer = tempBuffer;
                //Array.Copy(tempBuffer, this.buffer, tempBuffer.Length);




                return true;
            } else
            {
                this.buffer = new byte[BUFFER_SIZE];
                this.trie_header.next_allocated_location = TRIE_HEADER_SIZE;
                this.trie_header.trie_used_space = TRIE_HEADER_SIZE;
                this.trie_header.trie_items_count = 0;
                StructureToBuffer(this.trie_header, 0);
                return true;
            }
        }
        /// <summary>
        /// DefragBufferHelpFunction method implementation, this is a helper method of <see cref="DefragBuffer()"/> which takes part as recursion in <see cref="DefragBuffer()"/>, for() loop.
        /// </summary>
        private void DefragBufferHelpFunction(ref byte[] tempBuffer, short NodeLocation)
        {
            NodeInfo node = BufferToNodeInfo(NodeLocation);
            if(node.children_location == 0)
            {
                // copy node_array to tempBuffer
                // copy node_key_array to tempBuffer
                // copy node_value_array to tempBuffer
                // increase buffer used_space and alloc_location

                // get trie_header from temp buffer
                this.trie_header = BufferToTrieInfo(ref tempBuffer);
                // get node_size
                short node_size = SizeOfNode(NodeLocation);
                // copy from buffer to temp buffer

                // modify nodes value_location & key location
                short nodeOldValueLocation = 0;
                if(node.value_location > node.key_location + node.key_size - node.key_size % 8 + 8)
                {
                    nodeOldValueLocation = node.value_location;
                }
                node.key_location = (short)(this.trie_header.next_allocated_location + 8);
                node.value_location = (short)(node.key_size - node.key_size % 8 + 8 + node.key_location);

                Array.Copy(this.buffer.Skip(NodeLocation).Take(node_size).ToArray(),
                0,
                tempBuffer,
                this.trie_header.next_allocated_location,
                node_size);
                StructureToBuffer(node, this.trie_header.next_allocated_location, ref tempBuffer);


                if (nodeOldValueLocation != 0)
                {
                    Array.Copy(this.buffer.Skip(nodeOldValueLocation).Take(sizeof(long)).ToArray(),
                                0,
                                tempBuffer,
                                node.value_location,
                                sizeof(long));
                }


                // increase buffer used_space and alloc_location
                this.trie_header.next_allocated_location = (short)(this.trie_header.next_allocated_location + node_size);
                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space + node_size);
                StructureToBuffer(this.trie_header, 0, ref tempBuffer);

            }
            else if(node.children_location > 0)
            {
                // copy node_array to tempBuffer
                // copy node_key_array to tempBuffer
                // copy node_value_array to tempBuffer if there is one
                // copy node_children_array to tempBuffer
                // increase buffer used_space and alloc_location
                // for child in childs DefragBufferHelpFunction

                // get trie_header from temp buffer
                this.trie_header = BufferToTrieInfo(ref tempBuffer);
                // get node_size
                short node_size = SizeOfNode(NodeLocation);
                // copy from buffer to temp buffer
                short children_array_size = getChildUsedSpaceFromBuffer(node.children_location);
                //byte[] nodeArray = this.buffer.Skip(NodeLocation).Take(node_size - children_array_size).ToArray();

                byte[] node_children_array = this.buffer.Skip(node.children_location).Take(children_array_size).ToArray();


                // modify nodes value_location & key location & children location

                node.key_location = (short)(this.trie_header.next_allocated_location + 8);
                short nodeOldValueLocation = 0;
                if(node.value_location != 0)
                {
                    if (node.value_location < node.children_location) {
                        node.value_location = (short)(node.key_size - node.key_size % 8 + 8 + node.key_location);
                        node.children_location = (short)(sizeof(long) + node.value_location);
                        // value added after the node created
                    } else {
                        nodeOldValueLocation = node.value_location;
                        node.value_location = (short)(node.key_size - node.key_size % 8 + 8 + node.key_location);
                        node.children_location = (short)(sizeof(long) + node.value_location);
                    }
 
                } else
                {
                    node.children_location = (short)(node.key_size - node.key_size % 8 + 8 + node.key_location);
                }
                Array.Copy(this.buffer.Skip(NodeLocation).Take(node_size).ToArray(),
                0,
                tempBuffer,
                this.trie_header.next_allocated_location,
                (node_size));

                if(nodeOldValueLocation != 0)
                {
                    Array.Copy(this.buffer.Skip(nodeOldValueLocation).Take(sizeof(long)).ToArray(),
                                0,
                                tempBuffer,
                                node.value_location,
                                sizeof(long));
                }



                StructureToBuffer(node, this.trie_header.next_allocated_location, ref tempBuffer);

                short currentNodeLocation = this.trie_header.next_allocated_location;

                // increase buffer used_space and alloc_location
                this.trie_header.next_allocated_location = (short)(this.trie_header.next_allocated_location + node_size);
                this.trie_header.trie_used_space = (short)(this.trie_header.trie_used_space + node_size);

                StructureToBuffer(this.trie_header, 0, ref tempBuffer);

                for (int i = 2; i < node_children_array.Length; i = i + 2)
                {
                    short childNodeLocation = ToShort(node_children_array[i], node_children_array[i + 1]);

                    // change child location to next_allocated_location
                    this.trie_header = BufferToTrieInfo(ref tempBuffer);
                    byte byte1, byte2;
                    FromShort(this.trie_header.next_allocated_location, out byte1, out byte2);
                    node_children_array[i] = byte1;
                    node_children_array[i + 1] = byte2;


                    DefragBufferHelpFunction(ref tempBuffer, childNodeLocation);
                }
                // write node children array to temp buffer
                Array.Copy(node_children_array,
                0,
                tempBuffer,
                currentNodeLocation + node_size - node_children_array.Length,
                node_children_array.Length);
                //children_array_size

            }
        }
        /// <summary>
        /// SizeOfNode method implementation, the method returns the space of the given node in buffer array.
        /// </summary>
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
        /// <summary>
        /// InsertNodesToTrie method implementation,  this is a helper method of <see cref="TryWrite(string, long)"/> which takes parts of the given key and add them as complete nodes.
        /// </summary>
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
        /// <summary>
        /// checkMatch method implementation,  this is a helper method of <see cref="TryWrite(string, long)"/>, <see cref="TryRead(string, out long)"/> and <see cref="Delete(string)"/> methods, which recursively goes to the given key and returns its location in the buffer and the part of the key that it are.
        /// </summary>
        private Tuple<int, int> checkMatch(int nodeLocation, string key, int keyPosition)
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
        /// <summary>
        /// CheckNodeChildrenForMatch method implementation,  this is a helper method of <see cref="TryWrite(string, long)"/>, <see cref="TryRead(string, out long)"/>, <see cref="Delete(string)"/> and <see cref="checkMatch(int, string, int)"/> methods, which checks the given node childrens for a match between the first char of given key.
        /// </summary>
        private int CheckNodeChildrenForMatch(string keyFirstChar, int nodeLocation)
        {

            NodeInfo root_node = BufferToNodeInfo(nodeLocation);
            if(root_node.children_location == 0)
            {
                return -1;
            }
            int childrenNumber = ToShort(buffer[root_node.children_location], buffer[root_node.children_location + 1]);
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
        /// <summary>
        /// ReduceChildrenArray method implementation,  this is a helper method of <see cref="Delete(string)"/> which deletes the nodeLocation of one of the deleted nodes.
        /// </summary>
        private byte[] ReduceChildrenArray(byte[] arr, short deleteNodeLocation)
        {
            // temp array
            byte[] temp = new byte[arr.Length - sizeof(short)];
            byte byte1, byte2;
            //FromShort(deleteNodeLocation, out byte1, out byte2);
            //Console.WriteLine("deleteNodeLocation    " + deleteNodeLocation + "  ");
            int k = 0;
            for (int i = 0; i < arr.Length; i = i + 2)
            {
                //Console.WriteLine("node_locations    " + ToShort(arr[i], arr[i + 1]) + "  ");
                if(deleteNodeLocation == ToShort(arr[i], arr[i + 1]))
                {
                    k = i;
                }
                //ToShort(arr[i], arr[i + 1]);
            }

            //NodeInfo nodeq = BufferToNodeInfo(deleteNodeLocation);
            //string foundKeyz = BufferToString(nodeq.key_location, nodeq.key_size);
            //Console.WriteLine("deleteNodeLocation  foundKey    " + foundKeyz + "  ");
            //Console.WriteLine("arr.Length  size    " + arr.Length + "  ");
            //Console.WriteLine("temp  size    " + temp.Length + "  ");

            //int p = Array.IndexOf(arr, byte1);
            //Console.WriteLine("p    " + p + "   K   "+ k);

            int j = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (i != k && i != k + 1)
                {
                    temp[j] = arr[i];
                    j++;
                }

            }
            //for(int i=0; i< arr.Length; i = i + 2)
            //{
            //    NodeInfo node = BufferToNodeInfo(ToShort(arr[i], arr[i + 1]));
            //    string foundKey = BufferToString(node.key_location, node.key_size);
            //    Console.WriteLine("arr  foundKey    " + foundKey + "  ");

            //}
            //for (int i = 0; i < temp.Length; i = i + 2)
            //{
            //    NodeInfo node = BufferToNodeInfo(ToShort(temp[i], temp[i + 1]));
            //    string foundKey = BufferToString(node.key_location, node.key_size);
            //    Console.WriteLine("temp  foundKey    " + foundKey + "  ");
            //}

            FromShort((short)(ToShort(arr[0], arr[1]) - 1), out byte1, out byte2);
            temp[0] = byte1; temp[1] = byte2;
            return temp;
        }
        /// <summary>
        /// ResizeChildrenArray method implementation,  this is a helper method of <see cref="TryWrite(string, long)"/> which creates or expands children array in given location.
        /// </summary>
        private byte[] ResizeChildrenArray(byte[] arr, short newChildArrayLocation, string str)
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
        /// <summary>
        /// FromShort method implementation, this is a widely used method to return byte value of short
        /// </summary>
        private void FromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number >> 0);
        }
        /// <summary>
        /// ToShort method implementation, this is a widely used method to return short value of given bytes
        /// </summary>
        private short ToShort(byte byte1, byte byte2)
        {
            return (short)((byte2 << 8) | (byte1 << 0));
        }
        /// <summary>
        /// StringToBuffer method implementation, this is a widely used method to write given string to byte array at given location
        /// </summary>
        private void StringToBuffer(string str, int location)
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
        /// <summary>
        /// BufferToString method implementation, this is a method to read string from given location in buffer
        /// </summary>
        private string BufferToString(int keyLocation, short keySize)
        {
            short alignedKeySize = (short)(keySize - (keySize % 8) + 8);
            string str = Encoding.UTF8.GetString(buffer.Skip(keyLocation).Take(keySize).ToArray());
            return str;
        }
        /// <summary>
        /// ValueToBuffer method implementation, this is a widely used method to write nodes value to buffer
        /// </summary>
        private void ValueToBuffer(long num, int location)
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
        /// <summary>
        /// ShortToBuffer method implementation, this is a method to write short to buffer
        /// </summary>
        private void ShortToBuffer(short num, int location)
        {
            byte byte1, byte2;
            FromShort(num, out byte1, out byte2);
            buffer[location] = byte1;
            buffer[location + 1] = byte2;
        }
        /// <summary>
        /// getChildUsedSpaceFromBuffer method implementation, this is a helper method to calculate nodes children array used space in buffer
        /// </summary>
        private short getChildUsedSpaceFromBuffer(int location)
        {
            short children_number = ToShort(this.buffer[location], this.buffer[location + 1]);
            return (short)(sizeof(short) * children_number + sizeof(short));
        }
        /// <summary>
        /// StructureToBuffer method implementation, this is a helper method to write Structs to given buffer
        /// </summary>
        private void StructureToBuffer(object obj, int copyLocation, ref byte[] buffer)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);
            arr.CopyTo(buffer, copyLocation);
        }
        /// <summary>
        /// StructureToBuffer method implementation, this is a helper method to write Structs to trie buffer
        /// </summary>
        private void StructureToBuffer(object obj, int copyLocation)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);
            arr.CopyTo(this.buffer, copyLocation);
        }
        /// <summary>
        /// BufferToTrieInfo() method implementation, this is a helper method to read a Trie header from buffer
        /// </summary>
        private TrieInfo BufferToTrieInfo()
        {
            TrieInfo str = new TrieInfo();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(this.buffer, 0, ptr, size);

            str = (TrieInfo)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
        /// <summary>
        /// BufferToTrieInfo()  method implementation, this is a helper method to read a Trie header from a GIVEN buffer, used in <see cref="DefragBuffer()"/> function.
        /// </summary>
        private TrieInfo BufferToTrieInfo(ref byte[] arr)
        {
            TrieInfo str = new TrieInfo();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (TrieInfo)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
        /// <summary>
        /// BufferToNodeInfo method implementation, this is a helper method to read a node header from buffer
        /// </summary>
        private NodeInfo BufferToNodeInfo(int copyLocation)
        {
            NodeInfo str = new NodeInfo();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(this.buffer, copyLocation, ptr, size);

            str = (NodeInfo)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
        /// <summary>
        /// BufferToNodeInfo method implementation, this is a helper method to read a node header from given buffer, used in <see cref="DefragBuffer()"/> function.
        /// </summary>
        private NodeInfo BufferToNodeInfo(int copyLocation, ref byte[] arr)
        {
            NodeInfo str = new NodeInfo();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, copyLocation, ptr, size);

            str = (NodeInfo)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
        /// <summary>
        /// GetAvaibleSpace() method implementation, this is a helper method to get free space in buffer
        /// </summary>
        private int GetAvaibleSpace()
        {
            return BUFFER_SIZE - ToShort(buffer[0], buffer[1]);
        }
    }
}