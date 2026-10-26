-- ============================================================================
-- SURVIVAL 01 - 10-PLAYER BATTLE ROYALE SUPABASE SCHEMA
-- Chạy script này trong Supabase SQL Editor (https://supabase.com/dashboard)
-- Project URL: https://ganxpdiwtwtwpvdgodbi.supabase.co
-- ============================================================================

-- 1. Bảng hồ sơ và thống kê người chơi (profiles)
create table if not exists public.profiles (
    id uuid primary key,
    display_name text not null default 'Survivor',
    matches_played integer not null default 0,
    wins integer not null default 0,
    kills integer not null default 0,
    deaths integer not null default 0,
    top3_finishes integer not null default 0,
    total_damage integer not null default 0,
    updated_at timestamptz not null default now()
);

alter table public.profiles enable row level security;

-- Cho phép mọi người xem bảng xếp hạng
drop policy if exists "Profiles are viewable by everyone" on public.profiles;
create policy "Profiles are viewable by everyone"
    on public.profiles for select
    using (true);

-- Cho phép client tạo/cập nhật thống kê (hỗ trợ cả tài khoản Auth và Guest ID)
drop policy if exists "Anyone can insert profile" on public.profiles;
create policy "Anyone can insert profile"
    on public.profiles for insert
    with check (true);

drop policy if exists "Anyone can update own profile" on public.profiles;
create policy "Anyone can update own profile"
    on public.profiles for update
    using (true);

-- 2. Bảng lịch sử trận đấu (match_history)
create table if not exists public.match_history (
    id bigint generated always as identity primary key,
    player_id uuid not null,
    display_name text not null default 'Survivor',
    placement integer not null default 10,
    kills integer not null default 0,
    damage_dealt integer not null default 0,
    survived_seconds integer not null default 0,
    mode text not null default 'online_10p',
    created_at timestamptz not null default now()
);

alter table public.match_history enable row level security;

drop policy if exists "Match history viewable by everyone" on public.match_history;
create policy "Match history viewable by everyone"
    on public.match_history for select
    using (true);

drop policy if exists "Anyone can insert match history" on public.match_history;
create policy "Anyone can insert match history"
    on public.match_history for insert
    with check (true);

-- 3. Bảng danh sách phòng chờ trực tuyến (rooms)
create table if not exists public.rooms (
    room_code text primary key,
    host_name text not null default 'Host',
    host_ip text not null default '127.0.0.1',
    host_port integer not null default 7777,
    relay_join_code text not null default '',
    current_players integer not null default 1,
    max_players integer not null default 10,
    status text not null default 'waiting',
    updated_at timestamptz not null default now()
);

alter table public.rooms enable row level security;

drop policy if exists "Rooms viewable by everyone" on public.rooms;
create policy "Rooms viewable by everyone"
    on public.rooms for select
    using (true);

drop policy if exists "Anyone can upsert rooms" on public.rooms;
create policy "Anyone can upsert rooms"
    on public.rooms for insert
    with check (true);

drop policy if exists "Anyone can update rooms" on public.rooms;
create policy "Anyone can update rooms"
    on public.rooms for update
    using (true);

drop policy if exists "Anyone can delete rooms" on public.rooms;
create policy "Anyone can delete rooms"
    on public.rooms for delete
    using (true);

-- 4. Trigger tự động tạo hồ sơ trong public.profiles khi đăng ký qua Supabase Auth
create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer set search_path = public
as $$
begin
    insert into public.profiles (id, display_name)
    values (
        new.id,
        coalesce(new.raw_user_meta_data->>'display_name', split_part(new.email, '@', 1), 'Survivor')
    )
    on conflict (id) do nothing;
    return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
    after insert on auth.users
    for each row execute procedure public.handle_new_user();

