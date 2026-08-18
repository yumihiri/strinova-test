"""玉対戦ゲーム - 土台 + 基本UI (要件定義書セクション10の1・2)

正方形フィールド内で2つの玉がランダム移動し、接触するとダメージを与え合う。
HPが0になった方が負け。CPU対CPUの観戦専用で、プレイヤーはリセットボタンのみ操作できる。
"""
import math
import random
import sys

import pygame

# ---- 画面・フィールド設定 ----
WINDOW_WIDTH = 640
WINDOW_HEIGHT = 760
FIELD_SIZE = 600
FIELD_LEFT = (WINDOW_WIDTH - FIELD_SIZE) // 2
FIELD_TOP = 100
FPS = 60

# ---- 玉のパラメータ ----
BALL_RADIUS = 22
INITIAL_HP = 100
BASE_SPEED = 3.0
DIRECTION_CHANGE_MIN_FRAMES = 30
DIRECTION_CHANGE_MAX_FRAMES = 90
INVINCIBLE_FRAMES = 30  # 連続ヒット防止の無敵時間
KNOCKBACK_SPEED = 7.0
KNOCKBACK_DIRECTION_LOCK_FRAMES = 15  # ノックバック直後にすぐ方向転換しすぎないための猶予
BASE_DAMAGE = 6
SPEED_DAMAGE_FACTOR = 2.5

# ---- 色 ----
WHITE = (245, 245, 245)
BLACK = (20, 20, 20)
GRAY = (120, 120, 120)
FIELD_BG = (235, 235, 225)
FIELD_BORDER = (60, 60, 60)
RED = (220, 70, 70)
BLUE = (70, 120, 220)
HP_BG = (60, 60, 60)
HP_GREEN = (80, 190, 90)
HP_YELLOW = (230, 200, 60)
HP_RED = (220, 70, 70)
BUTTON_BG = (90, 90, 100)
BUTTON_HOVER = (120, 120, 135)
OVERLAY = (0, 0, 0)


class Ball:
    """対戦キャラクター(玉)。ランダム移動と体当たり衝突のみを扱う。"""

    def __init__(self, x, y, color, name):
        self.x = x
        self.y = y
        self.color = color
        self.name = name
        self.radius = BALL_RADIUS
        self.max_hp = INITIAL_HP
        self.hp = INITIAL_HP
        self.speed = BASE_SPEED
        self.vx = 0.0
        self.vy = 0.0
        self.direction_timer = 0
        self.invincible_timer = 0
        self.alive = True
        self._pick_new_direction()

    def _pick_new_direction(self, min_frames=None, max_frames=None):
        angle = random.uniform(0, 2 * math.pi)
        self.vx = math.cos(angle) * self.speed
        self.vy = math.sin(angle) * self.speed
        lo = DIRECTION_CHANGE_MIN_FRAMES if min_frames is None else min_frames
        hi = DIRECTION_CHANGE_MAX_FRAMES if max_frames is None else max_frames
        self.direction_timer = random.randint(lo, hi)

    def update(self):
        if not self.alive:
            return

        self.direction_timer -= 1
        if self.direction_timer <= 0:
            self._pick_new_direction()

        if self.invincible_timer > 0:
            self.invincible_timer -= 1

        self.x += self.vx
        self.y += self.vy

        min_x = FIELD_LEFT + self.radius
        max_x = FIELD_LEFT + FIELD_SIZE - self.radius
        min_y = FIELD_TOP + self.radius
        max_y = FIELD_TOP + FIELD_SIZE - self.radius

        if self.x < min_x:
            self.x = min_x
            self.vx = abs(self.vx)
        elif self.x > max_x:
            self.x = max_x
            self.vx = -abs(self.vx)

        if self.y < min_y:
            self.y = min_y
            self.vy = abs(self.vy)
        elif self.y > max_y:
            self.y = max_y
            self.vy = -abs(self.vy)

    def take_damage(self, amount):
        if not self.alive:
            return
        self.hp -= amount
        if self.hp <= 0:
            self.hp = 0
            self.alive = False

    def draw(self, screen):
        color = self.color if self.alive else GRAY
        if self.alive and self.invincible_timer > 0 and (self.invincible_timer // 3) % 2 == 0:
            color = tuple(min(255, c + 70) for c in color)
        pos = (int(self.x), int(self.y))
        pygame.draw.circle(screen, color, pos, self.radius)
        pygame.draw.circle(screen, BLACK, pos, self.radius, 2)


def circles_overlap(a, b):
    dx = b.x - a.x
    dy = b.y - a.y
    dist = math.hypot(dx, dy)
    return dist < (a.radius + b.radius), dist, dx, dy


def clamp_to_field(ball):
    min_x = FIELD_LEFT + ball.radius
    max_x = FIELD_LEFT + FIELD_SIZE - ball.radius
    min_y = FIELD_TOP + ball.radius
    max_y = FIELD_TOP + FIELD_SIZE - ball.radius
    ball.x = max(min_x, min(max_x, ball.x))
    ball.y = max(min_y, min(max_y, ball.y))


def resolve_collision(a, b):
    """体当たり衝突判定。ダメージ・ノックバック・無敵時間を処理する。"""
    if not a.alive or not b.alive:
        return
    if a.invincible_timer > 0 or b.invincible_timer > 0:
        return

    overlapping, dist, dx, dy = circles_overlap(a, b)
    if not overlapping:
        return

    if dist == 0:
        dx, dy = 1.0, 0.0
        dist = 1.0
    nx, ny = dx / dist, dy / dist

    # ヒット時の勢い(相対速度)が大きいほどダメージが大きい
    rel_speed = math.hypot(a.vx - b.vx, a.vy - b.vy)
    damage = int(BASE_DAMAGE + rel_speed * SPEED_DAMAGE_FACTOR)
    a.take_damage(damage)
    b.take_damage(damage)

    a.invincible_timer = INVINCIBLE_FRAMES
    b.invincible_timer = INVINCIBLE_FRAMES

    # めり込み解消
    overlap = (a.radius + b.radius) - dist
    push = overlap / 2 + 1
    a.x -= nx * push
    a.y -= ny * push
    b.x += nx * push
    b.y += ny * push
    clamp_to_field(a)
    clamp_to_field(b)

    # ノックバック(押し出し)
    a.vx, a.vy = -nx * KNOCKBACK_SPEED, -ny * KNOCKBACK_SPEED
    b.vx, b.vy = nx * KNOCKBACK_SPEED, ny * KNOCKBACK_SPEED
    a.direction_timer = KNOCKBACK_DIRECTION_LOCK_FRAMES
    b.direction_timer = KNOCKBACK_DIRECTION_LOCK_FRAMES


def make_balls():
    margin = FIELD_SIZE // 4
    ball_a = Ball(FIELD_LEFT + margin, FIELD_TOP + margin, RED, "RED")
    ball_b = Ball(FIELD_LEFT + FIELD_SIZE - margin, FIELD_TOP + FIELD_SIZE - margin, BLUE, "BLUE")
    return ball_a, ball_b


def hp_bar_color(ratio):
    if ratio > 0.5:
        return HP_GREEN
    if ratio > 0.2:
        return HP_YELLOW
    return HP_RED


def draw_hp_bar(screen, font, x, y, width, height, ball, align_right=False):
    label = font.render(f"{ball.name}  HP {ball.hp}/{ball.max_hp}", True, BLACK)
    label_x = x if not align_right else x + width - label.get_width()
    screen.blit(label, (label_x, y - 22))

    pygame.draw.rect(screen, HP_BG, (x, y, width, height))
    ratio = ball.hp / ball.max_hp if ball.max_hp else 0
    fill_width = int(width * ratio)
    if fill_width > 0:
        fill_rect = (x, y, fill_width, height) if not align_right else (x + width - fill_width, y, fill_width, height)
        pygame.draw.rect(screen, hp_bar_color(ratio), fill_rect)
    pygame.draw.rect(screen, BLACK, (x, y, width, height), 2)


def get_result_text(ball_a, ball_b):
    if not ball_a.alive and not ball_b.alive:
        return "引き分け!"
    if not ball_a.alive:
        return f"{ball_b.name} の勝ち!"
    if not ball_b.alive:
        return f"{ball_a.name} の勝ち!"
    return None


def main():
    pygame.init()
    screen = pygame.display.set_mode((WINDOW_WIDTH, WINDOW_HEIGHT))
    pygame.display.set_caption("玉対戦ゲーム")
    clock = pygame.time.Clock()

    title_font = pygame.font.SysFont("ipagothic", 28)
    hp_font = pygame.font.SysFont("ipagothic", 18)
    result_font = pygame.font.SysFont("ipagothic", 40)
    button_font = pygame.font.SysFont("ipagothic", 22)

    ball_a, ball_b = make_balls()
    winner_text = None

    button_rect = pygame.Rect(0, 0, 160, 44)
    button_rect.centerx = WINDOW_WIDTH // 2
    button_rect.y = FIELD_TOP + FIELD_SIZE + 16

    running = True
    while running:
        mouse_pos = pygame.mouse.get_pos()
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                running = False
            elif event.type == pygame.MOUSEBUTTONDOWN and event.button == 1:
                if button_rect.collidepoint(event.pos):
                    ball_a, ball_b = make_balls()
                    winner_text = None

        if winner_text is None:
            ball_a.update()
            ball_b.update()
            resolve_collision(ball_a, ball_b)
            winner_text = get_result_text(ball_a, ball_b)

        # ---- 描画 ----
        screen.fill(WHITE)

        title = title_font.render("玉対戦ゲーム", True, BLACK)
        screen.blit(title, (WINDOW_WIDTH // 2 - title.get_width() // 2, 12))

        bar_width = 240
        bar_height = 22
        draw_hp_bar(screen, hp_font, FIELD_LEFT, 66, bar_width, bar_height, ball_a, align_right=False)
        draw_hp_bar(screen, hp_font, FIELD_LEFT + FIELD_SIZE - bar_width, 66, bar_width, bar_height, ball_b, align_right=True)

        pygame.draw.rect(screen, FIELD_BG, (FIELD_LEFT, FIELD_TOP, FIELD_SIZE, FIELD_SIZE))
        pygame.draw.rect(screen, FIELD_BORDER, (FIELD_LEFT, FIELD_TOP, FIELD_SIZE, FIELD_SIZE), 3)

        ball_a.draw(screen)
        ball_b.draw(screen)

        hover = button_rect.collidepoint(mouse_pos)
        pygame.draw.rect(screen, BUTTON_HOVER if hover else BUTTON_BG, button_rect, border_radius=8)
        pygame.draw.rect(screen, BLACK, button_rect, 2, border_radius=8)
        button_label = button_font.render("リセット", True, WHITE)
        screen.blit(button_label, (button_rect.centerx - button_label.get_width() // 2,
                                    button_rect.centery - button_label.get_height() // 2))

        if winner_text is not None:
            overlay = pygame.Surface((FIELD_SIZE, FIELD_SIZE), pygame.SRCALPHA)
            overlay.fill((*OVERLAY, 120))
            screen.blit(overlay, (FIELD_LEFT, FIELD_TOP))
            result_surface = result_font.render(winner_text, True, WHITE)
            screen.blit(result_surface, (WINDOW_WIDTH // 2 - result_surface.get_width() // 2,
                                          FIELD_TOP + FIELD_SIZE // 2 - result_surface.get_height() // 2))

        pygame.display.flip()
        clock.tick(FPS)

    pygame.quit()
    sys.exit()


if __name__ == "__main__":
    main()
