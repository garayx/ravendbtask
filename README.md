# ravendbtask
Hi, this is my first attempt to code in c#.

The idea is to implement a trie via byte array.

The key is stored with separate node for each part of the key, therefore it take a bit too much space to store a key:
```
   root
  h    k
hi|val  ke
         key|val
```

Each node got 8 byte of header, then the part of the key aligned by 8 byte, 8 bytes of value (if there is) and a children number and locations.
```
[node header][part of key][value if there is][children array]
```
The trie header and root node are located in first 16 bytes.

The complete solution with tests class is in the rar file.

