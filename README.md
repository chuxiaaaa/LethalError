# LethalError 致命错误

---

[![Thunderstore Version](https://img.shields.io/thunderstore/v/chuxiaaaa/LethalError?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/chuxiaaaa/LethalError/versions/)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/chuxiaaaa/LethalError?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/chuxiaaaa/LethalError/)


This is a mod designed to fix `An error occured!` on the client side. 

这是一个用于修复客户端发生错误的模组。

It is a server-side mod and does not need to be installed on the client!  

这是一个服务端模组，客户端不需要安装！

Expected Effects (Examples):  

预期效果（例子）：

- When a vanilla client joins a lobby with LethalCasino installed, it will display

    当原版客户端加入安装 LethalCasino 的大厅，则会显示

    <img width="728" height="523" alt="img1" src="https://github.com/user-attachments/assets/fd847e72-5b82-4fce-b435-0f35da3575d5" />

- When a client with LethalCasino installed joins a vanilla lobby, it will display

    当安装 LethalCasino 的客户端加入原版大厅，则会显示

    <img width="721" height="520" alt="img2" src="https://github.com/user-attachments/assets/aad97164-9999-4eac-80ec-8f60cdb76057" />

- When the installed mods on the client cannot be identified, it will display
  
    无法识别客户端安装了什么模组，则会显示

    <img width="730" height="519" alt="img3" src="https://github.com/user-attachments/assets/e967dd49-2ec0-4141-a608-a78de7142b02" />

- When the client is vanilla and the host’s installed mods cannot be identified, it will display

    当客户端是原版的情况下，无法识别主机安装了什么模组，则会显示

    <img width="733" height="529" alt="img4" src="https://github.com/user-attachments/assets/f34f2e79-741b-449f-a396-b53318984aac" />

  
Both of these examples would normally trigger `An error occured!` message in vanilla gameplay.

这些例子在原版运行都是会提示发生错误的情况。

## Config 配置

Mod detection mainly relies on this configuration file. If you’re not proficient with it, it’s not recommended to make any changes.

识别模组主要靠的就是该配置文件，如果你对此不精通，不建议做任何改动

Even if the configuration file is not updated, it will not affect players joining the lobby or other gameplay. It only affects the prompts provided by `LethalError`!

即使配置文件没有更新，也不会影响玩家加入大厅等等行为，他只会影响`LethalError`给出的提示！

In the LethalError.dll directory, you can see the `LethalError.Mods.yml` file.

在 LethalError.dll 目录下 你可以看见 `LethalError.Mods.yml` 文件

`VanillaHash` is the hash value of the vanilla game.

`VanillaHash` 是原版游戏下的hash值

After a game update, `VanillaHash` may change. You can download the latest `LethalError.Mods.yml` file from GitHub to avoid issues.

当游戏版本更新后，`VanillaHash`可能会出现变动，你可以在 Github 中下载最新的`LethalError.Mods.yml`文件，来避免出现问题

Of course, you can also update it manually. Enable the `Debug` configuration in `chuxia.LethalError.cfg`. Then, make sure you are running the vanilla game and create a lobby. At this point, a `{hash}.txt` file will appear in the `LethalError.dll` directory, where {hash} represents the hash value of the vanilla game.

当然，你也可以手动更新。将`chuxia.LethalError.cfg`的`Debug`配置开启。并且确保你是原版游戏的情况下，创建一个大厅。此时在 LethalError.dll 目录下将会多出一个{hash}.txt文件，这个{hash}代表的就是原版游戏的值

If you open this file, you will see that it contains YAML-formatted content. When you have mods installed, it will automatically write any non-vanilla prefabs into this file.

如果你打开该文件，你就会发现里面是yml格式的内容。在你安装模组的情况下，他会将不是原版的Prefab自动写入到该文件内

How to identify an unsupported mod? Make sure you have only that mod installed. Then, copy the ModName and Prefabs from the above file into LethalError.Mods.yml. (If there are dependencies, you need to manually remove the prefabs of the dependencies.)

如何对一个未支持的模组进行识别？就是确保你只安装了该模组。将上诉文件的`ModName`以及`Prefabs`复制到`LethalError.Mods.yml`即可（如果有前置，你需要手动删掉前置的prefabs）
