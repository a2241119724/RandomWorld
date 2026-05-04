 # RandomWorld(未完成)

* 可能缺少某些资源
* 需要整个Build文件夹运行exe

## 进行中

* 多存档
* 工人按照现实打工人事件行为
* (material与Build) 优化Item\*\*：<通过读取ItemDataSO实例化Common\*\*>代替<通过父类获取子类创建新的道具>
* 任务树（优化）
* 种植任务
* 修改房间断定

## 注

* transform.Find("a/b/c")可获取active为false的对象
* photon的rpc同步时，大量数据不建议使用buffer，因为缓存有上限
* 必须上传\*.meta文件，不然配置出问题
* 修改完prefab之后需要重新打AB包
* RuleTile以y=x对称
* 在Button的UI界面添加点击函数, 需要先将脚本放到物体上，再添加该物体
* 道具数据ItemData与地图瓦片Tile的名称关联绑定
* 数据传输: Character -> Weapon -> WeaponEffect -> Character

## 其他

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\.gitarchive\Set-ArchivePassword.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\.gitarchive\Restore-EncryptedProjectArchive.ps1 -Force
```
