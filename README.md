# Домашнее задание
## Основное задание
Вам дан код JPEG подобного сжатия (проект JPEG), вам нужно максимально, насколько это возможно, оптимизировать его, в том числе уменьшить потребление памяти.

Пробовал много разных оптимизаций. Получил 5 место по скорости исполнения и 7 место по памяти из 30 человек.

Рекомендации:
* Профилируйте код (используйте dotTrace)
* Для начала оптимизируйте загрузку изображений и переписывайте только неэффективный код
* Пишите бенчмарки на разные методы
* Не бойтесь математики

С разными вопросами можно писать @Golrans, @ryzhes или @Jewry

Подсказки:
* Распаралельте DCT
* CbCr subsampling
* Используйте указатели, вместо GetPixel/SetPixel, придётся написать unsafe код
* Замените DCT на FFT (System.Numerics.Complex), нельзя использовать библиотеки, только собственная реализация!
* Помимо подсказанного в проекте ещё много узких мест (╯°□°）╯︵ ┻━┻

Как сдавать задание:
1. Нужно сделать замер через JpegProcessorBenchmark до оптимизаций и запомнить Mean и Allocated по операциям Compress и Uncompress
2. Сделать аналогичные замеры после оптимизаций
3. Внести свой результат в таблицу в день дедлайна, ссылку на которую вам дадут позже
4. Очно или онлайн за 10-15 минут рассказать какие моменты удалось найти и как оптимизировать

## Полезные ссылки
* [Про new()](https://devblogs.microsoft.com/premier-developer/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/)
* [Про IEquatable](https://devblogs.microsoft.com/premier-developer/performance-implications-of-default-struct-equality-in-c/)
* [Про Inlining методов](https://web.archive.org/web/20200108171755/http://blogs.microsoft.co.il/sasha/2012/01/20/aggressive-inlining-in-the-clr-45-jit/)
* [Презентация оптимизация](https://docs.google.com/presentation/d/1RA7HEMllcvjX9e4KLJu2LC-DHFqZvGj-hT9_hWTSgE8/edit?usp=sharing)
