# Windows-білд Forestio

Готова гра збирається у `Builds/Forestio-Windows/Forestio.exe` для Windows x64. До збірки входить сцена Outskirts. Режим Development Build вимкнений.

## Повторна збірка

У редакторі: **Forestio → Build → Windows release**.

Для автоматичної збірки використовується метод `ForestioWindowsBuild.Build`. Змінна середовища `FORESTIO_BUILD_PATH` дозволяє вказати інший абсолютний шлях до виконуваного файлу. Без неї використовується папка Builds у корені проєкту.

Перед передачею гри скопіювати Docs/START_HERE.txt з інструкціями до папки гри та заархівувати її повністю. Для запуску розпакувати весь архів і відкрити Forestio.exe. Файли Forestio_Data, UnityPlayer.dll та інші компоненти мають залишатися поряд з EXE.

Папка Builds виключена з Git через Unity .gitignore. У репозиторії зберігається скрипт для відтворення білду; готовий ZIP передається окремо.
