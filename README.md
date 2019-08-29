# Играем в paper.io

В этом репозитории вы найдете решение для контеста
[paper.io](https://github.com/MailRuChamps/miniaicups/tree/master/paperio).

Рещение заняло 1-е место в песочнице и предфинальном раунде, 2-е
место в финальном, и 3-е место в окончательном, после отбора 12 лучших.

## Начало

Контест начался с отладочного этапа, организаторы выкатили
репозиторий с local-runner-ом на питоне. По сути, все время до
официального начала многие участники (и я в том числе) занимались 
тем, что пытались разобраться в коде local-runner-а, найти в нем
баги и странности. Найденные важные проблемы:

- Неправильный учет времени - решениям участников одновременно
отправлялось состояние, а затем они последовательно опрашивались
с заявленным таймаутом в 5 секунд. В результате второе решение
начинало думать сразу, а его время начинало бежать только после
ответа первого.

- Алгоритм заливки - самое странное и противоречивое место во всем 
коде. Плохо в нем не столько то, что он имеет огромную вычислительную
сложность, а то, что его нельзя описать простыми словами.

При этом поведение алгоритма заливки практически тождественно 
простому алгоритму,
который участники прозвали "BFS от краев карты". Все очень просто:

- Добавляем в очередь все клеточки границы, кроме клеток шлейфа и клеток
территории игрока.

- После этого - обычный BFS - выбираем очередную клеточку
из очереди и добавляем в очередь ее соседей, так же кроме клеток
шлейфа и территории.

- Все клеточки, который мы таким образом покрасили -
это те, которые игрок не залил, остальные, следовательно, залил.

Убедить организаторов взять этот алгоритм не удалось, но многие
участники взяли его себе в симулятор. Либо взяли какую-то его
оптимизированную разновидность.

Я обычно пишу симулятор, тождественный тому, который дают организаторы,
но на C#, так сделал и в этот раз.
Это часто помогает мне разобраться в нюансах игры, найти ошибки, и получить
какую-то интуицию по поводу того, как можно решать поставленную
задачу. В реальных моих стратегиях он используется редко, там обычно
приходится писать что-то оптимизированное, а в данном контесте - еще и 
приближенное.

Код портированного симулятора можно посмотреть в проекте
[BrutalTester](https://github.com/spaceorc/aicup2019-paperio/tree/master/BrutalTester).
Название проекта говорит о том, что я хотел использовать его, чтобы гонять
много игр между разными стратегиями локально или на кластере, но в этот
раз это не пригодилось.

## Базовая идея

За время старта мне в голову долго не приходило ничего умного. По большому счету,
все вертелось вокруг двух мыслей. Первая - сделать минимакс, убирая из рассмотрения 
далеких игроков. Вторая - выбирать каким-то образом случайные пути,
симулировать, выбирать по какой-то оценке сучшее решение.

С первой идеей все более менее понятно, вторая, хоть и выглядит проще,
оставляет за кадром много вопросов. Как выбирать эти пути? Как учитывать
соперников? Например, можно считать, что соперники будут стараться мешать,
стараться пересечь наш шлейф. Но что это значит? Они будут бежать к ближайшей
точке предполагаемого шлейфа?

Вот тут сработала интуиция. Что, если мы будем выбирать безопасные пути, такие, чтобы
соперники не могли помешать их построить? Как же построить такой путь? Довольно
просто, оказывается.

Что значит, что соперник помешал нам построить путь? Предположим, нам
надо 10 тиков времени чтобы добежать до своей территории и закончить путь.
10 тиков - это много или мало? Путь - это набор клеток шлейфа. До каждой такой клеточки
соперник может добежать за какое-то время. Значит, если есть клеточки шлейфа, до которых
он может добежать за 9 тиков и менее, то такой путь нам не подходит.


Итак, верхнеуровневый алгоритм:
1. Строим карту времени - важная часть всей этой логики - знание, за какое 
время соперник добежит до какой-то клеточки.
2. Строим случайный безопасный путь с использованием этой карты.
3. Симулируем движение вдоль него. Враги, по построению, не могут нам 
помешать, значит, они просто бегут домой на свою территорию по кратчайшему
 пути.
4. Оцениваем результат какой-то оценочной функцией, выбираем лучшее решение.
5. Продолжаем с пункта 2, пока еще есть время.

Реализация этого алгоритма находится в классе 
[RandomWalkAi](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/RandomWalkAi.cs)

Здесь есть несколько кирпичиков, рассмотрим их всех по порядку.

## Мир

Для начала, в каком виде мы будем представлять себе мир. Мир - это карта 
31 на 31 клеточку. На ней расположено от одного до 6 ботов. Каждый бот
может либо находиться в какой-то клеточке, либо находиться на пути из одной
клеточки в соседнюю. Поэтому позиция бота представляется тройкой:

- `pos` - позиция, где бот стоит, если он стоит в клеточке, либо позиция,
из которой он ушел, если он в пути из одной клеточки в другую.
- `arrivePos` - равна `pos` если бот стоит в клеточке, либо позиция, куда он
скоро прибудет, если он находится в пути.
- `arriveTime` - время прибытия в `arrivePos`, 0 если бот стоит в клеточке

Кроме этих свойств у бота будут еще:
- `speed` - скорость бота.
- `shiftTime` - обратная величина к скорости бота - время, за которое
бот проходит целую клеточку. Равна 6 при скорости 5, равна 5 при скорости 6,
равна 10 при скорости 3.
- `nitroLeft` - сколько осталось дейтсвовать нитро.
- `slowLeft` - сколько осталось действовать замедлялке.

Далее я в примерах кода буду использовать класс `V` - вектор `(x,y)`, но
в реальном коде координата у меня представлена так:
```c#
ushort position = y * 31 + x;
```

С такими типами все работает обычно значительно быстрее, так как часто удается
полностью избежать GC.

## Карта времени

Итак, нам надо знать, в какой момент времени до куда может добраться каждый
игрок.

Первая, наивная идея - посчитаем манхеттенское расстояние между `arrivePos`
бота, умножим его на скорость бота, добавим `arriveTime`.

Код (точнее, конечно, псевдокод, здесь и далее):
```c#
var times = new Dictionary<V, int>();
for (var x = 0; x < 31; ++x)
{
    for (var y = 0; y < 31; ++y)
    {
        var v = new V(x, y);
        var dist = (v - bot.arrivePos).ManhattanLength();
        times[v] = bot.arriveTime + dist * bot.shiftTime;
    }
}
```

Что не учитывает этот подход? Во первых, скорость бота может меняться,
он может подбирать бонусы, его бонусы могут использоваться и исчезать.
Во вторых, бот не может проходить через свой шлейф, потому мы можем
переоценивать его способность нам помешать и наш бот будет черезчур
пугливым. В третьих, не учтено, что бот не может повернуть на 180 градусов
и пойти назад. 

Улучшаем идею - сначала разберемся с собственным шлейфом. Это просто,
достаточно пустить волну от `arrivePos` бота, запретив ему заходить
на свой шлейф.

```c#
var times = new Dictionary<V, int>();
var queue = new Queue<V>();
times.Add(bot.arrivePos, bot.arriveTime);
queue.Enqueue(bot.arrivePos);
while (queue.Any())
{
    var cur = queue.Dequeue();
    foreach (var next in cur.GetNeighbors())
    {
        if (IsOutsideMap(next) || IsOwnLine(next) || times.ContainsKey(next))
            continue;
        times.Add(next, times[cur] + bot.shiftTime);
        queue.Enqueue(next);
    }
}
```

Вот, раньше бот мог достичь любой клеточки, а теперь он не может пройти через
свой шлейф. Это проблема, потому что мы понимаем, что как только бот
достиг своей территории, он может идти куда угодно просто по прямой
(ну, то есть по манхеттенскому расстоянию).

Поэтому мы добавим в нашу карту
времени еще и точку, где бот впервые вернулся домой на свою территорию,
и дополнительно посчитаем время и по порвому алгоритму, от этой точки.
Пересечем две полученные карты и возьмем минимум - все, мы учли собственный
шлейф бота.

Следующее важное улучшение - учет бонусов.

Как учесть бонусы в первом подходе с движением по манхеттенскому 
расстоянию? Понятно, что подбор бонусов никак не учесть, потому что
мы никак не смотрим на состояние мира. Можно учесть только расход бонусов.
Нам известно сколько осталось действовать нитро и замедлялке, мы можем
разбить все расстояние на 4 части: 
- Сначала `min(nitroLeft, slowLeft)` - столько клеточек действуют оба
бонуса, следовательно скорость обычная, равная 5, и `shiftTime`, соответстсвенно,
равен 6.
- Затем `max(0, nitroLeft - slowLeft)` - столько клеточек действет только
нитро, следовательно скорость повышенная, равная 6, и `shiftTime`, соответственно,
равен 5.
- Затем `max(0, slowLeft - nitroLeft)` - столько клеточек действет только
замедлялка, следовательно скорость пониженная, равная 3, и `shiftTime`, соответственно,
равен 10.
- Оставшееся расстояние не действуют никакие бонусы, значит тоже все обычное.

Тонкий нюанс - если бот сейчас в пути, по по прибытии его бонусы уменьшатся
на единицу, надо не забыть это учесть.

Получилось следующее:
```c#
var times = new Dictionary<V, int>();
for (var x = 0; x < 31; ++x)
{
    for (var y = 0; y < 31; ++y)
    {
        var v = new V(x, y);
        var dist = (v - bot.arrivePos).ManhattanLength();

        var time = bot.arriveTime;

        var nitroLeft = bot.nitroLeft;
        var slowLeft = bot.slowLeft;
        if (bot.arriveTime > 0)
        {
            nitroLeft = Math.Max(0, nitroLeft - 1);
            slowLeft = Math.Max(0, slowLeft - 1);
        }

        var bothDist = Math.Min(dist, Math.Min(nitroLeft, slowLeft));
        time += bothDist * NORMAL_SHIFT_TIME;
        dist -= bothDist;
        
        var nitroDist = Math.Min(dist, Math.Max(0, nitroLeft - slowLeft));
        time += nitroDist * NITRO_SHIFT_TIME;
        dist -= nitroDist;
        
        var slowDist = Math.Min(dist, Math.Max(0, slowLeft - nitroLeft));
        time += slowDist * SLOW_SHIFT_TIME;
        dist -= slowDist;
        
        time += dist * NORMAL_SHIFT_TIME;

        times[v] = time;
    }
}
```

Ладно, теперь давайте придумаем, как учесть время во втором подходе,
с волной от `arrivePos` бота. Здесь мы уже бежим по миру и смотрим на его состояние.
Следовательно, сможем учесть подбор ботом бонусов.
Итак, тоже довольно простая идея. Будем строить вместе с картой времени также и карту,
на которой будем отмечать, сколько бонусов обоих типов осталось у бота. Если
бот приходит на клеточку, и там лежит бонус, увенличиваем соответствующее число,
если нет, уменьшаем на единицу.

Дополнительный нюанс - теперь мы можем прийти в клеточку второй раз с лучшим временем,
чем приходили до этого. Как это учесть? Можно по разному. Самый, на мой взгляд,
простой способ - это использовать вместо очереди - очередь с приоритетом. В
качестве приоритета использовать время. Это будет уже не BFS, а алгоритм Дейкстры,
но кого это волнует? :) На самом деле в первом раунде, в песочнице, у меня была
обычная очередь, в которую могли попасть одни и те же клеточки и второй и в третий раз,
но потом я переделал на очередь с приоритетом, и все упростилось.

```c#
var times = new Dictionary<V, int>();
var nitroLefts = new Dictionary<V, int>();
var slowLefts = new Dictionary<V, int>();
var priorityQueue = new PriorityQueue<int, V>();

var nitroLeft = bot.nitroLeft;
var slowLeft = bot.slowLeft;
if (bot.arriveTime > 0)
{
    nitroLeft = Math.Max(0, nitroLeft - 1);
    slowLeft = Math.Max(0, slowLeft - 1);
}

var start = bot.arrivePos;

nitroLefts[start] = nitroLeft;
slowLefts[start] = slowLefts;
times[start] = bot.arriveTime;
priorityQueue.Enqueue(bot.arriveTime, start);

while(priorityQueue.Any())
{
    var (curTime, cur) = priorityQueue.Dequeue();
    var curNitroLeft = nitroLefts[cur];
    var curSlowLeft = slowLefts[cur];
    var curShiftTime = CalculateShiftTime(curNitroLeft, curSlowLeft);
    foreach (var next in cur.GetNeighbors())
    {
        if (IsOutsideMap(next) || IsOwnLine(next))
            continue;
        var nextTime = curTime + curShiftTime;
        if (times.TryGetValue(next, out var prevTime) && prevTime < nextTime)
            continue;
        var nextNitroLeft = Math.Max(0, curNitroLeft - 1);
        var nextSlowLeft = Math.Max(0, curSlowLeft - 1);
        if (map[next].Bonus is Nitro)
            nextNitroLeft += GetActiveTicks(map[next].Bonus);
        if (map[next].Bonus is Slow)
            nextSlowLeft += GetActiveTicks(map[next].Bonus);
        times[next] = nextTime;
        nitroLefts[next] = nextNitroLeft;
        slowLefts[next] = nextSlowLeft;
        queue.Enqueue(nextTime, next);
    }
}
```

Здесь есть две вспомогательные функции - `CalculateShiftTime` и `GetActiveTicks`.
Первая реализуется очевидно:

```c#
int CalculateShiftTime(nitroLeft, slowLeft)
{
    if (nitroLeft > 0 && slowLeft > 0)
        return NORMAL_SHIFT_TIME;
    if (nitroLeft > 0)
        return NITRO_SHIFT_TIME;
    if (slowLeft > 0
        return SLOW_SHIFT_TIME;
    return NORMAL_SHIFT_TIME;
}
```

Вторая предназначена для того, чтобы посчитать, на сколько клеточек действует
бонус. Из правил не было понятно, приходит ли эта информация в состоянии. И
в local-runner-е она приходила. Поэтому я учел оба варианта, и эта функция
просто берет значение из состояния мира, если оно там есть, и вычисляет, есмли его
там нет. Вычисляет так - если это нитро, то для моего бота бонус действует 
минимально возможное время - 10 клеточек, а для врагов - максимально возможное -
50 клеточек. Для замедлялки, соответственно, все наоботрот. Такие умолчания
позволяют надежнее учесть подбираемые бонусы и не ошибиться случайно.

```c#
int GetActiveTicks(Bonus bonus)
{
    if (bonus.ActiveTicks != null)
        return bonus.ActiveTicks;
    if (bonus is Nitro)
        return player == ME ? 10 : 50;
    if (bonus is Slow)
        return player == ME ? 50 : 10;
}
```

Описанный выше подход, совмещающий построение карты времени через очередь
и по манхеттенскому расстоянию я использовал в песочнице. Все прочие улучшения
были сделаны на более поздних этапах.

Дополнительные возможности, реализующиеся похожим образом:

- Длина шлейфа бота, когда он входит в клеточку и длина шлейфа, когда он полностью
вошел в нее.
- Моменты вода бота в клеточку и покидания ботом клеточки.
- Направления, с которых он может в клеточку войти.

Все эти вещи позволяют точнее учесть, грозит ли нам смертью столкновение 
с ботом в этой клеточке во все моменты возможного столкновения - оба входят или
один догоняет, второй убегает.

Кроме того, вторая часть построения карты времени, конечно, тоже должна быть
сделана через очередь с приоритетом вместо манхеттенского расстояния, что
позволяет учесть подбор бонусов.

Также, попутно мы строим пути до всех клеток поля, что пригодится, енсли надо будет бота
отправить в какое-то место.

Все это реализовано в классе [DistanceMap](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/DistanceMap.cs)

## Безопасный путь

Вторая важная часть - это построение безопасного пути.

Рассмотрим, для начала, ситуацию, когда мы уже выбежали со своей территории,
имеем какой-то шлейф, хотим построить какой-то безопасный путь, возвращающий
нас на свою территорию.

Повторим логику:
1. Определяем `timeLimit` - время, за которое мы должны завершить путь на 
своей территории - определяем это по карте времени - это минимальное время, за которое 
вражеские боты могут дойти до любой клеточки нашего шлейфа.
2. Текущая клеточка - та, на которой стоим сейчас. Текущее время `time` на завершение 
пути - 0.
3. Перебираем случайным образом соседние клеточки с текущей, определяем ту,
которую можно добавить
к нашему пути:
    - Новое время - текущее время + `shiftTime` бота из текущей клеточки - его легко 
    определить - можно поддерживать, чтолько у бота осталось нитро и замедлялок, а также
    у нас есть функция `CalculateShiftTime`, описанная выше.
    - Новый лимит времени - минимум из `timeLimit` и времени, за которое боты могут дойти
    до новой добавляемой клеточки.
    - Если новое время больше нового лимита времени, то мы точно не успели, эта
    клеточка-кандидат не подходит.
    - Если новое время равно новому лимиту времени, то тут зависит от того, завершим
    ли мы путь, добавив эту клеточку - то есть, является ли она частью нашей территории,
    если да, то все ок, закраска имеет приоритет перед перерезанием шлейфа.
4. Если мы перебрали все соседние клеточки и ничего добавить не смогли, то все,
путь построить не удалось, надо начинать сначала.

Как случайным образом перебирать соседние клеточки? Интуиция говорит нам, что
надо предпочитать пути без поворотов, поэтому просто упорядочим варианты направлений
так, чтобы наиболее вероятно первым направлением выбиралось направление "вперед", а 
"влево" и "вправо" были равновероятны. В моем алгоритме направление "вперед" берется
в качестве первого с вероятностью 0.9.

Итак, все просто, инициализация и получение исходного лимита времени:

```c#
var currentTime = 0;
var current = bot.arrivePos;
var nitroLeft = bot.nitroLeft;
var slowLeft = bot.slowLeft;
var timeLimit = MAX_TICK_COUNT - currentTime;
var lineLength = bot.lineLength;
foreach (var v in bot.Line)
{
    if (distanceMap.times[v] < timeLimit)
        timeLimit = distancaMap.times[v];
}
```

Здесь в качестве исходного лимита времни берется время,
оставшееся до конца игры. Это позволяет завершать пути
в конце игры, а не оставаться в пространстве с недобранными
очками.

И алгоритм добавления очередной клеточки к пути:

```c#
bool TryAdd(V next)
{
    var shiftTime = CalculateShiftTime(nitroLeft, slowLeft);
    var nextTime = currentTime + shiftTime;
    var nextTimeLimit = Math.Min(timeLimit, distanceMap.times[next]);

    if (nextTime > nextTimeLimit)
        return false;
    if (nextTime == nextTimeLimit)
    {
        if (map[next].owner != ME)
            return false;
    }

    var nextNitroLeft = Math.Max(0, nitroLeft - 1);
    var nextSlowLeft = Math.Max(0, slowLeft - 1);
    
    if (map[next].bonus is Nitro)
        nextNitroLeft = GetActiveTicks(map[next].bonus);
    if (map[next].bonus is Slow)
        nextSlowLeft = GetActiveTicks(map[next].bonus);

    nitroLeft = nextNitroLeft;
    slowLeft = nextSlowLeft;
    currentTime = nextTime;
    current = next;
    timeLimit = nextTimeLimit; 
    lineLength++;
    return true;
}
```

Псевдокод написан в предположении что враг у нас один и мы смотрим на карту времени для него, понятно
что в реальности должен быть цикл по врагам.

Что же, пришла пора начать ходить по краю. Пересчет лимита времени работает хорошо, мы знаем когда мы должны 
закончить. Вопрос - сравнение `if (nextTime == nextTimeLimit)` выше - оно описывает какую ситуацию?
Очевидно, что оно описывает две различнных ситуации:
1. Мы встали на очередную клеточку и в это время враг где-то наступил нам на хвост - если мы при этом завершили
путь, то это нормально.
2. Мы встали на клеточку и в это же время враг встал на ЭТУ ЖЕ клеточку - совсем не очевидно, можем мы добавить ее
к пути или нет.

Второй вариант - это подмножество разнообразных вариантов столкновения, и их надо рассмотреть каждый по отдельности.

Если помните, мы в карте времени расчитали когда бот входит в клеточку, когда покидает ее, с какой стороны
может войти, какие бонусы при этом имеет.

Давайте рассмотрим для начала варианты нашей смерти:
1. Мы еще входим в клеточку и враг еще входит в клеточку, мы при этом пересеклись - наши шлейфы еще не увеличены,
значит мы умрем, если наш шлейф длиннее или такой же как у врага на момент входа в клеточку.
2. Мы убегаем из клеточки, но не успеваем убежать, враг входит. Когда он пересекается с нами?
    - Либо он входит в направлении отличеом от нашего, тогда сразу пересекается 
    и надо сравнивать длину шлейфа врага на входе в клеточку
    и наш шлейф после ее посещения - то есть увеличенный на 1.  
    - Либо он входит в направлении, совпадающем с нашим, тогда мы можем убежать
    от него, достаточно сравнить наше время покидания клеточки и его время полного
    захода в клеточку. Все эти времена для врага, а также направления,
    с которых он может зайти в клеточку, мы заботливо посчитали в `distanceMap`
    
Мы рассмотрели ситуацию, когда наш бот уже убежал со своей территории. Теперь 
давайте рассмотрим, что же делать, если бот еще на своей территории, куда ему бежать?

Возможны несколько подходов, и на разных этапах соревнования я использовал
разные.

Самый наивный подход, который играл в первом раунде - просто идем в сторону ближайшей, еще
не занятой нами клеточки, не важно кому она принадлежит, нам или врагу.
Поиск такой клеточки и пути к ней легко встраивается в карту времени.

Второй подход, тоже предельно простой, играл у меня до самого конца, как
запасной вариант, если не сработал более сложный. Второй подход - это идти
к ближайшей точке, занятой врагом - это тоже легко встраивается в карту времени.
Почему это лучше? Потому что за захват территории врага мы получаем
в 5 раз больше очков. Интуиция говорит, что надо быть поближе к его территории.

Третий подход заключается в том, чтобы начинать генерировать безопасный путь,
еще находясь на своей территории. Ничего страшного, что наш исходный лимит времени
большой, как только мы при поиске пути выйдем за границу своей территории,
все нормализуется.

Вся эта логика, включая варианты хождения по краю, реализованы в классе 
[ReliablePathBuilder](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/ReliablePathBuilder.cs).

Генератор случайных путей, использующий `ReliablePathBuilder`, реализован
в классе [RandomPathGenerator](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/RandomPathGenerator.cs)

Какие еще есть ситуации, которые влияют на качество нашего безопасного пути?
Вот мы ищем пути, которые возвращают нас на нашу территорию. А что, если
этой территории осталось быть нашей совсем недолго? Из этого есть два следствия,
очевидное и не вполне:

1. Если какой-то нашей территории осталось жить меньше чем наш текущий лимит
времени то заход на нее не завершает путь, надо искать дальше.
2. Если мы еще не вышли с нашей территории, то переход на клеточку, которая 
к этому моменту перестанет быть нашей как раз начнет наш шлейф, и надо
начинать считать лимит времени.

Осталось понять, как посчитать время жизни территории.

Я применил примитивный подход. Он заключается в том, что прежде всего
мы симулируем для всех ботов движение на их собственную территорию
по кратчайшему пути, для каждой клеточки запоминаем, когда она перестала
быть нашей. Это очень простой и неточный подход, но он значительно улучшил
мое решение. Можно сделать еще лучше, расчитать для каждого бота различные пути
возвращения на свою территорию, а не только кратчайшие, но это я не успел сделать
за время контеста.

Логика расчета времени жизни территории реализована в классе
[InterestingFacts](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/InterestingFacts.cs).

## Оценочная функция

Мы научились качественно выбирать безопасный путь, симулируем проход по нему,
врагов симулируем так, что они просто идут к своей территории по кратчайшему пути.
Теперь надо как-то научиться выбирать лучшее решение.

Самая первая моя оценочная функция была очень простой - она просто считала
количество очков. Такая функция хорошо играет, когда
врагов уже нет, и надо успеть набрать как можно больше, пока игра не закончилась.
Кроме этого, она награждала за убитых врагов. Реализация находится в классе
[BestScoreEstimator](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/PathEsimators/BestScoreEstimator.cs).

Вторая функция, с улучшениями, играла у меня до самого конца. В первую очередь
предпочитаем убивать врагов. Затем предпочитаем собирать как можно больше
клеточек вражеской территории. Если ни одной не захватили, откатываемся на оценку
по счету. Если захватили, то смотрим на затраченное время, чем меньше, тем лучше.
В самом конце, при прочих равных, оцениваем заработанные очки. Реализация
находится в классе 
[CaptureOpponentEstimator](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/PathEsimators/CaptureOpponentEstimator.cs).

Идея третьей функции возникла у меня, как ответ на игру,
в которой мой бот мог завершить территорию чуть раньше
и тем не дать врагу завершить свою.
Идея в том, чтобы заранее посчитать, сколько территории захватит враг, если просто
симулировать движение всех по кратчайшему пути домой, а потом, в оценочной 
функции, досимулировать всех до возвращения домой также по кратчайшему пути,
и учесть в оценочной функции эту разницу. В бою это решение показало себя хуже. Оказалось,
что в среднем лучше забить на врагов и заботиться только о том, как самому заработать
больше. Возможно, идею можно было как-то доработать, но времени на это не было,
и я ее откатил. Тем, не менее, реализация
находится в классе 
[CaptureAndPreventOpponentEstimator](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/RandomWalk/PathEsimators/CaptureAndPreventOpponentEstimator.cs).

Дополнительно удалось улучшить оценочную функцию, введя в нее учет собранных
бонусов. Понятно, что надо награждать за собранные нитро, и штрафовать за
замедлялки. Я ввел это таким образом - если берешь нитро, то получаешь
бонус в виде сэкономленного времени - сколько клеточек в среднем действует нитро,
умноженное на разницу во времени прохождения этих клеточек с нитро и без,
то есть `30 * (NORMAL_SHIFT_TIME - NITRO_SHIFT_TIME)`. Аналогично вводится штраф за 
замедление, только коэффициент я выбрал чуть побольше - 50 вместо 30, он
показал лучший результат. На самом деле, конечно, все это эвристические константы.
Никакой внятной статистики я не собирал.

Еще одно улучшение - реакция на следующую проблему - когда бот не находит решения,
позволяющего захватить хоть одну вражескую клеточку, он начинает искать путь,
приносяий максимум очков. Оказалось, что бот долго, шагов 300-400 способен строить
огромную территорию совсем без вражеских клеток. Это плохо - за такую территорию получаешь 
мало очков, и по сути это трата времени. В результате я просто ввел в оценочную
функцию огрничение на предельную время построения такого вот "пустого" шлейфа.
Я взял значение этого ограничения в 200 тиков, подобрав его просто по результатам
просмотра игр. Оценочная функция стала просто жестко штрафовать за превышение 
этого времени, и бот перестал бесцельно бродить.

## Minimax

За время контеста я также написал и минимакс. Тут у нас не два игрока, а шесть,
кроме того, ходят они не по очереди, а одновременно. Поэтому пришлось сделать
следующее:
1. Перебираем конечно же по очереди, причем, сначала хожу я, потом все остальные.
2. Перебор адаптивный по глубине, то есть запускаем его несколько раз со все 
увеличивающейся глубиной, пока не выйдет отведенное на перебор время.
3. Глубина - это количество полных ходов моего бота, то есть количество
клеточек, на которое он сместится в итоге.
4. Вражеские боты, находящиеся дальше расстояния, равного глубине, не перебираются,
а просто идут по кратчайшей территории к себе домой.

Получившийся алгоритм играл хуже моего основного, я пытался все оптимизировать.
Переписал полностью весь симулятор на unsafe структурах и указателях,
получилось быстрее и даже заработало, но в реальных сценариях количество перебираемых
конечных позиций увеличилось не сильно (28000 позиций в некоторой ситуации
превращалось в 33000), а вводить новые фичи и отлаживать гораздо сложнее
из-за особенностей IDE. А кроме того, надо было переделать на эти рельсы все существующее.
В результате откатил все это, осталось только в истории коммитов
(https://github.com/spaceorc/aicup2019-paperio/tree/unsafe-done/Game/Unsafe).

Тем не менее, минимакс пригодился, я сделал перебор вначале на 70 мс - с его помощью
искал недопустимые ходы, то есть, ходы, в результате которыхз меня могут убить.
Реально в таких условиях удается перебирать примерно на 3-4 полных хода вглубь,
и это сильно улучшило мое решение.

Кроме того, я сделал "убийство минимаксом", то есть если минимакс находил ход,
гарантировано убивающий хоть одного врага, я делал его. Правда, это тоже оказалось
плохим решением, если чтобы чтобы кого-то убить, надо потратить 3-4 хода, то
лучше этого не делать, можно заработать больше очков, захватывая территории.
В результате я "убийство мнинимаксом" убрал, оставил только простое эвристическое убийство,
когда врага можно убить одним ходом, если он неосторожно идет по моей территории,
и не успевает вернуться на свою. 

Реализация минимакса находится в классе
[Minimax](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/BruteForce/Minimax.cs),
а реализация поиска допустимых ходов на его основе в классе
[AllowedDirectionsFinder](https://github.com/spaceorc/aicup2019-paperio/tree/master/Game/Strategies/BruteForce/AllowedDirectionsFinder.cs),

## Заключение

В целом, на протяжении всего контеста казалось, что идеи исчерпаны, но всегда 
удавалось найти еще какую-то достаточно простую, и выйти на первое место.
Но вот под конец все же не удалось, на момент заморозки, у меня решение работало уже
почти 12 часов, а соперники еще сабмитили свои улучшения - и им удалось меня побить.

# Структура репозитория

- `BrutalTester` - портированный симулятор. Проект планировался для того, чтобы гонять
свои разные стратегии на кластере или локально друг против друга и выбирать лучшую, но 
это я так и не реализовал (хотя обычно в контестах это удается и помогает).
- `Pack` - упаковщик - собирает все из папки Game в один файл main.cs, пригодный
к отправке на сервер.
- `UnitTests` - некоторое (небольшое) множество тестов.
- `Game` - основной код решения:
    - `Protocol` - логика ввода/вывода.
    - `Helpers` - всякие вспомогательные штуки.
    - `Sim` - симулятор
    - `Strategies` - логика игры:
        - `BruteForce` - все что связано с минимаксом.
        - `RandomWalk` - все что связано с рандомным поиском:
            - `PathEstimators` - различные алгоритмы оценки
            качества пути.
            - `StartPathStrategies` - различные стратегии подхода
            к началу пути, если путь найти не удалось.
            - `DistanceMap.cs` - карта времени достижения каждым ботом каждой точки, также в ней хранятся кратчайшие пути
            до всех точек карты, длины шлейфов на входе и выходе из клеточек, время входа и выхода, количество бонусов.
            - `InterestingFacts.cs` - здесь предварительный расчет всяких штук, которые в разное время требовались для 
            разных частец алгоритма: время жизни клеточек территории, время и расстояние до пилы, сколько потенциально
            получит очков бот, если сейчас же начнет возвращаться домой.
            - `ReliablePathBuilder.cs` - механика построения безопасного пути.
            - `RandomPathGenerator.cs` - генератор случайного безопасного пути.
            - `RandomWalkAi.cs` - основной алгоритм поиска решения.
