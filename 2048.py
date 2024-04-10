import pygame
import sys
import random

# 初始化pygame
pygame.init()

# 设置屏幕大小
screen_width = 500
screen_height = 500

# 创建屏幕对象
screen = pygame.display.set_mode((screen_width, screen_height))

# 设置标题
pygame.display.set_caption('2048')

# 定义颜色
WHITE = (255, 255, 255)
GRAY = (200, 200, 200)
GREEN = (0, 255, 0)
RED = (255, 0, 0)

# 游戏区域大小
grid_size = 4
cell_size = screen_width // grid_size

# 初始化游戏板
board = [[0 for x in range(grid_size)] for y in range(grid_size)]

# 当前分数
score = 0

# 游戏结束标志
game_over = False

# 随机生成初始数字
def generate_random():
    global board, score
    available_cells = [(x, y) for x in range(grid_size) for y in range(grid_size) if board[x][y] == 0]
    if available_cells:
        cell = random.choice(available_cells)
        board[cell[0]][cell[1]] = random.choice([2, 4])
        if board[cell[0]][cell[1]] == 4:
            score += 4

# 绘制数字
def draw_number(x, y, number, color=WHITE):
    font = pygame.font.SysFont(None, 60)
    text = font.render(str(number), True, color)
    screen.blit(text, (x * cell_size + cell_size // 2 - text.get_width() // 2, y * cell_size + cell_size // 2 - text.get_height() // 2))

# 绘制游戏板
def draw_board():
    for x in range(grid_size):
        for y in range(grid_size):
            rect = pygame.Rect(x * cell_size, y * cell_size, cell_size, cell_size)
            pygame.draw.rect(screen, GRAY, rect, 1)
            if board[x][y] != 0:
                draw_number(x * cell_size, y * cell_size, board[x][y])

# 检查游戏是否结束
def check_game_over():
    global game_over
    global score
    for x in range(grid_size):
        for y in range(grid_size - 1):
            if board[x][y] == board[x][y + 1]:
                return False
        for y in range(grid_size):
            for x in range(grid_size - 1):
                if board[x][y] == board[x + 1][y]:
                    return False
    if any(cell >= 2048 for row in board for cell in row):
        return True
    return False

# 处理移动
# 合并一行中的数字
def merge_row(row):
    new_row = []
    skip = 0
    for i in range(len(row)):
        if row[i] == 0:
            continue
        if i == len(row) - 1 or row[i] == row[i + 1]:
            new_row.append(row[i] * 2)
            skip = 1
        else:
            new_row.append(row[i])
    return new_row[:-skip], skip

# 执行移动操作
def move(direction):
    global board, score
    for i in range(grid_size):
        if direction == UP or direction == DOWN:
            # 先合并每一行
            board[i], _ = merge_row(board[i])
            # 然后移动每一行
            if direction == UP:
                for j in range(grid_size - 1):
                    board[i][j], board[i][j + 1] = board[i][j + 1], board[i][j]
            elif direction == DOWN:
                for j in range(grid_size - 1, 0, -1):
                    board[i][j], board[i][j - 1] = board[i][j - 1], board[i][j]
        elif direction == LEFT or direction == RIGHT:
            # 转置游戏板
            for j in range(grid_size):
                for k in range(j + 1, grid_size):
                    if board[j][k] == 0:
                        board[j][k], board[k][j] = board[k][j], board[j][k]
            # 交换行和列
            board = list(zip(*board))
            # 应用上述逻辑到转置后的板
            board = [list(row) for row in board]
            # 再次转置回来
            board = list(zip(*board))

    # 检查并处理移动后的合并和新数字生成
    for i in range(grid_size):
        board[i], _ = merge_row(board[i])
    # 生成新数字
    for i in range(grid_size):
        for j in range(grid_size):
            if board[i][j] == 0:
                generate_random(i, j)

# 检查游戏是否结束
def check_game_over():
    global game_over
    global score
    for row in board:
        if any(数字 >= 2048 for 数字 in row):
            game_over = True
            return
    for i in range(grid_size):
        for j in range(i + 1, grid_size):
            if board[i][j] == board[i][j + 1] or any(a == b for a, b in zip(board[i], board[i + 1])):
                return False
    return True

# 随机生成新数字
def generate_random(x, y):
    global board, score
    if board[x][y] == 0:
        board[x][y] = random.choice([2, 4])
        if board[x][y] == 4:
            score += 4

# 主游戏循环
running = True
while running:
    # ...（其他代码保持不变）

    # 处理按键
    if keys[pygame.K_UP]:
        move(UP)
    elif keys[pygame.K_DOWN]:
        move(DOWN)
    elif keys[pygame.K_LEFT]:
        move(LEFT)
    elif keys[pygame.K_RIGHT]:
        move(RIGHT)

    if check_game_over():
        game_over = True
        running = False

    # ...（其他代码保持不变）



# 主游戏循环
running = True
while running:
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False

       if keys[pygame.K_UP]:
        move(UP)
    elif keys[pygame.K_DOWN]:
        move(DOWN)
    elif keys[pygame.K_LEFT]:
        move(LEFT)
    elif keys[pygame.K_RIGHT]:
        move(RIGHT)

    if check_game_over():
        game_over = True
        running = False

    # 绘制背景
    screen.fill(GRAY)

    # 绘制游戏板
    draw_board()

    # 绘制分数
    score_font = pygame.font.SysFont(None, 30)
    score_text = score_font.render('Score: ' + str(score), True, WHITE)
    screen.blit(score_text, (10, 10))

    # 更新屏幕
    pygame.display.flip()

    # 控制帧率
    pygame.time.Clock().tick(30)

pygame.quit()
sys.exit()