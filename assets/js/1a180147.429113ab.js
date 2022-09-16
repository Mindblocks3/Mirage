"use strict";(self.webpackChunkmirage_docs=self.webpackChunkmirage_docs||[]).push([[1183],{3905:(e,n,t)=>{t.d(n,{Zo:()=>c,kt:()=>h});var a=t(67294);function o(e,n,t){return n in e?Object.defineProperty(e,n,{value:t,enumerable:!0,configurable:!0,writable:!0}):e[n]=t,e}function r(e,n){var t=Object.keys(e);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);n&&(a=a.filter((function(n){return Object.getOwnPropertyDescriptor(e,n).enumerable}))),t.push.apply(t,a)}return t}function s(e){for(var n=1;n<arguments.length;n++){var t=null!=arguments[n]?arguments[n]:{};n%2?r(Object(t),!0).forEach((function(n){o(e,n,t[n])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(t)):r(Object(t)).forEach((function(n){Object.defineProperty(e,n,Object.getOwnPropertyDescriptor(t,n))}))}return e}function i(e,n){if(null==e)return{};var t,a,o=function(e,n){if(null==e)return{};var t,a,o={},r=Object.keys(e);for(a=0;a<r.length;a++)t=r[a],n.indexOf(t)>=0||(o[t]=e[t]);return o}(e,n);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);for(a=0;a<r.length;a++)t=r[a],n.indexOf(t)>=0||Object.prototype.propertyIsEnumerable.call(e,t)&&(o[t]=e[t])}return o}var l=a.createContext({}),p=function(e){var n=a.useContext(l),t=n;return e&&(t="function"==typeof e?e(n):s(s({},n),e)),t},c=function(e){var n=p(e.components);return a.createElement(l.Provider,{value:n},e.children)},d={inlineCode:"code",wrapper:function(e){var n=e.children;return a.createElement(a.Fragment,{},n)}},u=a.forwardRef((function(e,n){var t=e.components,o=e.mdxType,r=e.originalType,l=e.parentName,c=i(e,["components","mdxType","originalType","parentName"]),u=p(t),h=o,m=u["".concat(l,".").concat(h)]||u[h]||d[h]||r;return t?a.createElement(m,s(s({ref:n},c),{},{components:t})):a.createElement(m,s({ref:n},c))}));function h(e,n){var t=arguments,o=n&&n.mdxType;if("string"==typeof e||o){var r=t.length,s=new Array(r);s[0]=u;var i={};for(var l in n)hasOwnProperty.call(n,l)&&(i[l]=n[l]);i.originalType=e,i.mdxType="string"==typeof e?e:o,s[1]=i;for(var p=2;p<r;p++)s[p]=t[p];return a.createElement.apply(null,s)}return a.createElement.apply(null,t)}u.displayName="MDXCreateElement"},5625:(e,n,t)=>{t.r(n),t.d(n,{assets:()=>l,contentTitle:()=>s,default:()=>d,frontMatter:()=>r,metadata:()=>i,toc:()=>p});var a=t(87462),o=(t(67294),t(3905));const r={sidebar_position:6,title:"Spawn Object - Custom"},s="Custom Spawn Functions",i={unversionedId:"guides/game-objects/spawn-object-custom",id:"guides/game-objects/spawn-object-custom",title:"Spawn Object - Custom",description:"You can use spawn handler functions to customize the default behavior when creating spawned game objects on the client. Spawn handler functions ensure you have full control of how you spawn the game object, as well as how you destroy it.",source:"@site/docs/guides/game-objects/spawn-object-custom.md",sourceDirName:"guides/game-objects",slug:"/guides/game-objects/spawn-object-custom",permalink:"/Mirage/docs/guides/game-objects/spawn-object-custom",draft:!1,editUrl:"https://github.com/MirageNet/Mirage/tree/master/doc/docs/guides/game-objects/spawn-object-custom.md",tags:[],version:"current",sidebarPosition:6,frontMatter:{sidebar_position:6,title:"Spawn Object - Custom"},sidebar:"docs",previous:{title:"Spawn Object",permalink:"/Mirage/docs/guides/game-objects/spawn-object"},next:{title:"Spawn Object - Pooling",permalink:"/Mirage/docs/guides/game-objects/spawn-object-pooling"}},l={},p=[{value:"Setting Up a Game Object Pool with Custom Spawn Handlers",id:"setting-up-a-game-object-pool-with-custom-spawn-handlers",level:2},{value:"Dynamic spawning",id:"dynamic-spawning",level:2}],c={toc:p};function d(e){let{components:n,...t}=e;return(0,o.kt)("wrapper",(0,a.Z)({},c,t,{components:n,mdxType:"MDXLayout"}),(0,o.kt)("h1",{id:"custom-spawn-functions"},"Custom Spawn Functions"),(0,o.kt)("p",null,"You can use spawn handler functions to customize the default behavior when creating spawned game objects on the client. Spawn handler functions ensure you have full control of how you spawn the game object, as well as how you destroy it."),(0,o.kt)("p",null,"Use ",(0,o.kt)("inlineCode",{parentName:"p"},"ClientObjectManager.RegisterSpawnHandler")," or ",(0,o.kt)("inlineCode",{parentName:"p"},"ClientObjectManager.RegisterPrefab")," to register functions to spawn and destroy client game objects. The server creates game objects directly and then spawns them on the clients through this functionality. This function takes either the asset ID or a prefab and two function delegates: one to handle creating game objects on the client, and one to handle destroying game objects on the client. The asset ID can be a dynamic one, or just the asset ID found on the prefab game object you want to spawn."),(0,o.kt)("p",null,"The spawn/unspawn delegates will look something like this:"),(0,o.kt)("p",null,(0,o.kt)("strong",{parentName:"p"},"Spawn Handler")),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},"NetworkIdentity SpawnDelegate(SpawnMessage msg) \n{\n    // do stuff here\n}\n")),(0,o.kt)("p",null,(0,o.kt)("strong",{parentName:"p"},"UnSpawn Handler")),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},"void UnSpawnDelegate(NetworkIdentity spawned) \n{\n    // do stuff here\n}\n")),(0,o.kt)("p",null,"When a prefab is saved its ",(0,o.kt)("inlineCode",{parentName:"p"},"PrefabHash")," field will be automatically set. If you want to create prefabs at runtime you will have to generate a new Hash instead."),(0,o.kt)("p",null,(0,o.kt)("strong",{parentName:"p"},"Generate prefab at runtime")),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},'// Create a hash that can be generated on both server and client\n// using a string and GetStableHashCode is a good way to do this\nint coinHash = "MyCoin".GetStableHashCode();\n\n// register handlers using hash\nClientObjectManager.RegisterSpawnHandler(creatureHash, SpawnCoin, UnSpawnCoin);\n')),(0,o.kt)("admonition",{type:"note"},(0,o.kt)("p",{parentName:"admonition"},"The unspawn function may be left as ",(0,o.kt)("inlineCode",{parentName:"p"},"null"),", Mirage will then call ",(0,o.kt)("inlineCode",{parentName:"p"},"GameObject.Destroy")," when the destroy message is received.")),(0,o.kt)("p",null,(0,o.kt)("strong",{parentName:"p"},"Use existing prefab")),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},"// register handlers using prefab\nClientObjectManager.RegisterPrefab(coin, SpawnCoin, UnSpawnCoin);\n")),(0,o.kt)("p",null,(0,o.kt)("strong",{parentName:"p"},"Spawn on Server")),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},'int coinHash = "MyCoin".GetStableHashCode();\n\n// spawn a coin - SpawnCoin is called on client\n// pass in coinHash so that it is set on the Identity before it is sent to client\nNetworkServer.Spawn(gameObject, coinHash);\n')),(0,o.kt)("p",null,"The spawn functions themselves are implemented with the delegate signature. Here is the coin spawner. The ",(0,o.kt)("inlineCode",{parentName:"p"},"SpawnCoin")," would look the same, but have different spawn logic:"),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},"public NetworkIdentity SpawnCoin(SpawnMessage msg)\n{\n    return Instantiate(m_CoinPrefab, msg.position, msg.rotation);\n}\npublic void UnSpawnCoin(NetworkIdentity spawned)\n{\n    Destroy(spawned);\n}\n")),(0,o.kt)("p",null,"When using custom spawn functions, it is sometimes useful to be able to unspawn game objects without destroying them. This can be done by calling ",(0,o.kt)("inlineCode",{parentName:"p"},"NetworkServer.Destroy(identity, destroyServerObject: false)"),", making sure that the 2nd argument is false. This causes the object to be ",(0,o.kt)("inlineCode",{parentName:"p"},"Reset")," on the server and sends a ",(0,o.kt)("inlineCode",{parentName:"p"},"ObjectDestroyMessage")," to clients. The ",(0,o.kt)("inlineCode",{parentName:"p"},"ObjectDestroyMessage")," will cause the custom unspawn function to be called on the clients. If there is no unspawn function the object will instead be ",(0,o.kt)("inlineCode",{parentName:"p"},"Destroy")),(0,o.kt)("p",null,"Note that on the host, game objects are not spawned for the local client, because they already exist on the server. This also means that no spawn or unspawn handler functions are called."),(0,o.kt)("h2",{id:"setting-up-a-game-object-pool-with-custom-spawn-handlers"},"Setting Up a Game Object Pool with Custom Spawn Handlers"),(0,o.kt)("p",null,"you can use custom spawn handlers in order set up object pooling so you dont need to instantiate and destroy objects each time you use them. "),(0,o.kt)("p",null,"A full guide on pooling can be found here: ",(0,o.kt)("a",{parentName:"p",href:"./spawn-object-pooling"},"Spawn Object Pooling")),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},"void ClientConnected() \n{\n    clientObjectManager.RegisterPrefab(prefab, PoolSpawnHandler, PoolUnspawnHandler);\n}\n\n// used by clientObjectManager.RegisterPrefab\nNetworkIdentity PoolSpawnHandler(SpawnMessage msg)\n{\n    return GetFromPool(msg.position, msg.rotation);\n}\n\n// used by clientObjectManager.RegisterPrefab\nvoid PoolUnspawnHandler(NetworkIdentity spawned)\n{\n    PutBackInPool(spawned);\n}\n")),(0,o.kt)("h2",{id:"dynamic-spawning"},"Dynamic spawning"),(0,o.kt)("p",null,"Some times you may want to create objects at runtime and you might not know the prefab hash ahead of time. For this you can use Dynamic Spawn Handlers to return a spawn handler for a prefab hash."),(0,o.kt)("p",null,"Below is an example where client pre-spawns objects while loading, and then network spawns them when receiving a ",(0,o.kt)("inlineCode",{parentName:"p"},"SpawnMessage")," from server."),(0,o.kt)("p",null,"Dynamic Handler avoid the need to add 1 spawn handler for each prefab hash. Instead you can just add a single dynamic handler that can then be used to find and return objects."),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-cs"},'// store handler in field so that you dont need to allocate a new one for each DynamicSpawn call\nSpawnHandler _handler;\nList<NetworkIdentity> _preSpawnedObjects = new List<NetworkIdentity>();\n\nvoid Start() \n{\n    _handler = new SpawnHandler(FindPreSpawnedObject, null);\n    \n    // fill _preSpawnedObjects here with objects\n    _preSpawnedObjects.Add(new GameObject("name").AddComponent<NetworkIdentity>());\n}\n\npublic SpawnHandler DynamicSpawn(int prefabHash)\n{\n    if (IsPreSpawnedId(prefabHash))\n        // return a handler that is using FindPreSpawnedObject\n        return _handler;\n    else\n        return null;\n}\n\nbool IsPreSpawnedId(int prefabHash) \n{\n    // prefabHash starts with 16 bits of 0, then it an id we are using for spawning\n    // this chance of this happening randomly is very low    \n    // you can do more validation on the hash based on use case\n    return (prefabHash & 0xFFFF) == 0;\n}\n\n// finds object based on hash and returns it\npublic NetworkIdentity FindPreSpawnedObject(SpawnMessage spawnMessage)\n{\n    var prefabHash = spawnMessage.prefabHash.Value;\n    // we stored index in last 16 bits on hash\n    var index = prefabHash >> 16;\n    \n    var identity = _preSpawnedObjects[index];\n    return identity;\n}\n')))}d.isMDXComponent=!0}}]);