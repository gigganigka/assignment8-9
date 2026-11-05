# Проверка Unity Remote на Windows + iPhone

Открой НОВУЮ папку проекта в Unity 6000.6.2f1 и дождись компиляции. Не добавляй UnityRemoteSupport.cs внутрь Assets. Если ранее менял пакет вручную, эта версия проверит его: при работающем поиске класса повторный обработчик не устанавливается.

В Assets/Editor добавлен UnityRemoteCompatibility: он автоматически проверяет поддержку при загрузке редактора. Если пакет не находит GenericRemote, подключает его существующий обработчик к UnityEditor.GenericRemoteModule. Исходники установленного пакета не изменяются. Дополнительно можно вызвать Tools -> Mobile Game -> 7. Check Unity Remote Compatibility.

1. Подключи разблокированный iPhone по USB, подтверди доверие. iTunes должен видеть телефон.
2. Открой Unity Remote 5 на телефоне. В Project Settings -> Editor -> Unity Remote выбери iPhone.
3. После сообщения Unity Remote compatibility installed переподключи телефон / заново открой Unity Remote: необходим новый сеанс подключения.
4. Вне Play: Tools -> Mobile Game -> 1. Prepare Project and Bake UI.
5. Открой mainmenu, вкладку Game, нажми Play (Pause выключена).
6. Проверь Console: Unity Remote connected to input! Затем Input Debugger -> Devices: должен появиться Touchscreen.
7. Проверь PLAY, джойстик, JUMP и FIRE пальцами.

Если не работает, пришли сообщения Console со словами Remote compatibility и список Devices. Обходной обработчик использует внутренний API Unity и не был запущен здесь: Unity отсутствует в среде подготовки. Это не подтверждённое на устройстве исправление. В сборки Windows/Web/iOS этот Editor-скрипт не включается.
