

Getting started
---------------

*On the current version of the node*


1. Install .NET Core [1.0.0-preview2-1-003177](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.1-preview2.1-download.md)

    1.1. Install for Ubuntu 14.04, 16.04, 16.10 & Linux Mint 17 (64 bit)
    You don't need to download the above tarballs.  Instead, follow the [instructions here](https://www.microsoft.com/net/core#linuxubuntu), 
    and when you get to step 2, use the following command instead, for the correct version of .NET Core:
    ```
    sudo apt-get install dotnet-dev-1.0.0-preview2-1-003177
    ```

2. Clone the reposiroty 
```
git clone https://github.com/nitro-coin/NitrocoinFullNode.git 
```

3. Checkout the develop branch (or master without latest changes)
```
cd NitrocoinFullNode
git checkout develop
```

4. Restore the project packages  
```
dotnet restore
```

5. Run the node on Main net
```
cd Nitrocoin.BitcoinD
dotnet run
```

Docker Containers
-------------------
