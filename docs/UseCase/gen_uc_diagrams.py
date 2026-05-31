"""
Sinh 24 file .drawio riêng cho từng Use Case của dự án Mutants Arena.
Chạy: python gen_uc_diagrams.py
"""

import os

OUT_DIR = os.path.dirname(os.path.abspath(__file__))

# ──────────────────────────────────────────────────────────────
# Dữ liệu từng UC
# left_actors / right_actors : list of (id, label)
# includes                   : list of (id, code, name)   – UC này <<include>> vào đâu
# ──────────────────────────────────────────────────────────────
UC_DATA = [
    dict(id="uc01", code="UC01", name="Đăng ký tài khoản",
         left_actors=[("guest","Khách")], right_actors=[], includes=[]),
    dict(id="uc02", code="UC02", name="Đăng nhập / Khởi tạo phiên JWT",
         left_actors=[("guest","Khách"),("player","Người chơi")], right_actors=[], includes=[]),
    dict(id="uc03", code="UC03", name="Tạo nhân vật chính và vào game",
         left_actors=[("player","Người chơi")], right_actors=[],
         includes=[("uc02","UC02","Đăng nhập / Khởi tạo phiên JWT"),
                   ("unetauth","UNet01","Xác thực kết nối / Gán zone ban đầu")]),
    dict(id="uc04", code="UC04", name="Di chuyển qua portal / Chuyển map",
         left_actors=[("player","Người chơi")], right_actors=[],
         includes=[("unetzone","UNet03","Chuyển zone / Scene / Visibility")]),
    dict(id="uc05", code="UC05", name="Chiến đấu và dùng kỹ năng",
         left_actors=[("player","Người chơi")], right_actors=[("server","Máy chủ")],
         includes=[("unetsync","UNet02","Đồng bộ vị trí / Animation / Trạng thái")]),
    dict(id="uc06", code="UC06", name="Quản lý túi đồ và trang bị",
         left_actors=[("player","Người chơi")], right_actors=[], includes=[]),
    dict(id="uc07", code="UC07", name="Nâng cấp trang bị tại Blacksmith",
         left_actors=[("player","Người chơi")], right_actors=[],
         includes=[("uc06","UC06","Quản lý túi đồ và trang bị")], extends=[]),
    dict(id="uc08", code="UC08", name="Nâng Gene chính",
         left_actors=[("player","Người chơi")], right_actors=[], includes=[], extends=[]),
    dict(id="uc09", code="UC09", name="Chọn và nâng Gene phụ",
         left_actors=[("player","Người chơi")], right_actors=[],
         includes=[], extends=[("uc08","UC08","Nâng Gene chính")]),
    dict(id="uc10", code="UC10", name="Dung hợp Hybrid Gene",
         left_actors=[("player","Người chơi")], right_actors=[],
         includes=[("uc09","UC09","Chọn và nâng Gene phụ")], extends=[]),
    dict(id="uc11", code="UC11", name="Phân bổ tiềm năng và kỹ năng",
         left_actors=[("player","Người chơi")], right_actors=[], includes=[], extends=[]),
    dict(id="uc12", code="UC12", name="Tương tác NPC / Mua vật phẩm",
         left_actors=[("player","Người chơi")], right_actors=[], includes=[], extends=[]),
    dict(id="uc13", code="UC13", name="Nhận, theo dõi và hoàn thành nhiệm vụ",
         left_actors=[("player","Người chơi")], right_actors=[("server","Máy chủ")],
         includes=[], extends=[]),
    dict(id="uc14", code="UC14", name="Quản lý bạn bè",
         left_actors=[("player","Người chơi")], right_actors=[], includes=[], extends=[]),
    dict(id="uc15", code="UC15", name="Tạo và quản lý tổ đội",
         left_actors=[("player","Người chơi")], right_actors=[], includes=[], extends=[]),
    dict(id="uc16", code="UC16", name="Chat đa kênh thời gian thực",
         left_actors=[("player","Người chơi")], right_actors=[],
         includes=[], extends=[("uc15","UC15","Tạo và quản lý tổ đội")]),
    dict(id="uc17", code="UC17", name="Tạo, tham gia và hoàn tất phó bản",
         left_actors=[("player","Người chơi")], right_actors=[("server","Máy chủ")],
         includes=[("unetruntime","UNet04","Spawn quái / Boss / Đối tượng mạng")], extends=[]),
    dict(id="uc18", code="UC18", name="Xem và làm mới leaderboard",
         left_actors=[("player","Người chơi")], right_actors=[("admin","Quản trị viên")],
         includes=[], extends=[]),
    dict(id="uc19", code="UC19", name="Đăng ký, heartbeat và giải phóng server",
         left_actors=[("admin","Quản trị viên")], right_actors=[("server","Máy chủ")],
         includes=[], extends=[]),
    dict(id="uc20", code="UC20", name="Đăng ký host map / Phát thưởng dungeon",
         left_actors=[("admin","Quản trị viên")], right_actors=[("server","Máy chủ")],
         includes=[], extends=[("uc17","UC17","Tạo, tham gia và hoàn tất phó bản")]),
    dict(id="unetauth", code="UNet01", name="Xác thực kết nối / Gán zone ban đầu",
         left_actors=[], right_actors=[("server","Máy chủ")], includes=[], extends=[]),
    dict(id="unetsync", code="UNet02", name="Đồng bộ vị trí / Animation / Trạng thái",
         left_actors=[("player","Người chơi")], right_actors=[("server","Máy chủ")],
         includes=[], extends=[]),
    dict(id="unetzone", code="UNet03", name="Chuyển zone / Scene / Visibility",
         left_actors=[("player","Người chơi")], right_actors=[("server","Máy chủ")],
         includes=[], extends=[]),
    dict(id="unetruntime", code="UNet04", name="Spawn quái / Boss / Đối tượng mạng",
         left_actors=[], right_actors=[("server","Máy chủ")], includes=[], extends=[]),
]

# ──────────────────────────────────────────────────────────────
# Hàm sinh XML
# ──────────────────────────────────────────────────────────────
ACTOR_STYLE = (
    "shape=mxgraph.uml.actor;whiteSpace=wrap;html=1;"
    "fillColor=#ffffff;strokeColor=#000000;"
    "labelPosition=center;verticalLabelPosition=bottom;"
    "verticalAlign=top;fontSize=12;"
)
UC_STYLE   = "ellipse;whiteSpace=wrap;html=1;fontSize=12;fillColor=#dae8fc;strokeColor=#6c8ebf;"
INC_STYLE  = "ellipse;whiteSpace=wrap;html=1;fontSize=11;fillColor=#fff2cc;strokeColor=#d6b656;"
EXT_STYLE  = "ellipse;whiteSpace=wrap;html=1;fontSize=11;fillColor=#ffe6cc;strokeColor=#d79b00;"
INC_EDGE   = ("dashed=1;endArrow=open;endFill=0;html=1;fontSize=11;"
              "startFill=0;startArrow=none;")
EXT_EDGE   = ("dashed=1;endArrow=open;endFill=0;html=1;fontSize=11;"
              "strokeColor=#d79b00;fontColor=#d79b00;"
              "startFill=0;startArrow=none;")
ASSOC_EDGE = "endArrow=none;html=1;"

SYS_STYLE  = ("rounded=0;whiteSpace=wrap;html=1;fontSize=14;fontStyle=1;"
              "align=center;verticalAlign=top;spacingTop=8;"
              "fillColor=none;strokeColor=#000000;")

PAGE_W, PAGE_H = 820, 540

def cell(cid, value, style, x, y, w, h, vertex=True, parent="1", extra=""):
    tag = f'vertex="1"' if vertex else 'edge="1"'
    return (
        f'        <mxCell id="{cid}" value="{value}"\n'
        f'          style="{style}"\n'
        f'          {tag} parent="{parent}" {extra}>\n'
        f'          <mxGeometry x="{x}" y="{y}" width="{w}" height="{h}" as="geometry"/>\n'
        f'        </mxCell>\n'
    )

def edge(cid, value, style, src, tgt):
    v = f'value="{value}"' if value else 'value=""'
    return (
        f'        <mxCell id="{cid}" {v}\n'
        f'          style="{style}"\n'
        f'          edge="1" source="{src}" target="{tgt}" parent="1">\n'
        f'          <mxGeometry relative="1" as="geometry"/>\n'
        f'        </mxCell>\n'
    )

def make_diagram(uc):
    uid    = uc["id"]
    code   = uc["code"]
    name   = uc["name"]
    la     = uc["left_actors"]
    ra     = uc["right_actors"]
    incs   = uc["includes"]

    exts = uc.get("extends", [])
    has_includes = bool(incs)
    has_extends  = bool(exts)
    has_related  = has_includes or has_extends

    # ── Layout ──────────────────────────────────────────────
    # System boundary
    bx, by, bw, bh = 140, 40, 535, 460

    # Main UC – nếu có include/extend thì đặt lên trên
    uc_w, uc_h = 350, 60
    uc_cx = bx + bw // 2
    uc_y  = by + 90 if has_related else by + bh // 2 - uc_h // 2

    # Include UCs – hàng dưới; Extend UCs – hàng thứ 2 (hoặc cùng hàng nếu chỉ có 1 loại)
    all_related = [(cid, code, name, "inc") for cid, code, name in incs] + \
                  [(cid, code, name, "ext") for cid, code, name in exts]

    # Dùng cùng y row cho cả include lẫn extend (tách vùng trái/phải nếu cần)
    inc_y   = by + bh - 130
    inc_w, inc_h = 230, 55

    # Actors
    actor_w, actor_h = 40, 60

    left_y_start  = by + bh // 2 - (len(la) * 100) // 2 + 10
    right_y_start = by + bh // 2 - (len(ra) * 100) // 2 + 10
    left_x  = 40
    right_x = bx + bw + 65

    # ── Cells ───────────────────────────────────────────────
    cells = []

    # System boundary
    cells.append(cell("sys", "Hệ thống Mutants Arena", SYS_STYLE,
                       bx, by, bw, bh))

    # Main UC
    main_uc_label = f"{code}&#xa;{name}"
    cells.append(cell(f"main_{uid}", main_uc_label, UC_STYLE,
                       uc_cx - uc_w//2, uc_y, uc_w, uc_h))

    # Included UCs


    inc_ids = {}
    n_all = len(all_related)
    if n_all == 1:
        xs = [uc_cx - inc_w // 2]
    elif n_all == 2:
        gap = 20
        total = n_all * inc_w + (n_all - 1) * gap
        xs = [uc_cx - total//2 + i*(inc_w + gap) for i in range(n_all)]
    else:
        gap = 15
        total = n_all * inc_w + (n_all - 1) * gap
        xs = [bx + 20 + i*(inc_w + gap) for i in range(n_all)]

    for i, (rel_id, rel_code, rel_name, rel_type) in enumerate(all_related):
        node_id = f"rel_{rel_id}"
        inc_ids[rel_id] = node_id
        label = f"{rel_code}&#xa;{rel_name}"
        style = INC_STYLE if rel_type == "inc" else EXT_STYLE
        cells.append(cell(node_id, label, style, xs[i], inc_y, inc_w, inc_h))
        if rel_type == "inc":
            e_label = "&lt;&lt;include&gt;&gt;"
            e_style = INC_EDGE
        else:
            e_label = "&lt;&lt;extend&gt;&gt;"
            e_style = EXT_EDGE
        cells.append(edge(f"e_rel_{uid}_{i}", e_label,
                          e_style, f"main_{uid}", node_id))

    # Left actors
    edge_idx = 0
    for i, (act_id, act_label) in enumerate(la):
        ay = left_y_start + i * 100
        cells.append(cell(f"la_{act_id}_{uid}", act_label, ACTOR_STYLE,
                          left_x, ay, actor_w, actor_h))
        cells.append(edge(f"e_la_{uid}_{edge_idx}", "",
                          ASSOC_EDGE, f"la_{act_id}_{uid}", f"main_{uid}"))
        edge_idx += 1

    # Right actors
    for i, (act_id, act_label) in enumerate(ra):
        ay = right_y_start + i * 100
        cells.append(cell(f"ra_{act_id}_{uid}", act_label, ACTOR_STYLE,
                          right_x, ay, actor_w, actor_h))
        cells.append(edge(f"e_ra_{uid}_{i}", "",
                          ASSOC_EDGE, f"ra_{act_id}_{uid}", f"main_{uid}"))

    body = "".join(cells)

    xml = f"""\
<mxfile host="app.diagrams.net" modified="2026-05-24T00:00:00.000Z" agent="GitHub Copilot" version="24.7.17">
  <diagram id="{uid}-detail" name="{code} - {name}">
    <mxGraphModel dx="900" dy="620" grid="0" gridSize="10" guides="1"
      tooltips="1" connect="1" arrows="1" fold="1" page="1"
      pageScale="1" pageWidth="{PAGE_W}" pageHeight="{PAGE_H}"
      math="0" shadow="0">
      <root>
        <mxCell id="0"/>
        <mxCell id="1" parent="0"/>
{body}
      </root>
    </mxGraphModel>
  </diagram>
</mxfile>
"""
    return xml


def main():
    created = []
    for uc in UC_DATA:
        xml      = make_diagram(uc)
        filename = f"{uc['id']}_usecase.drawio"
        filepath = os.path.join(OUT_DIR, filename)
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(xml)
        created.append(filename)
        print(f"  ✔  {filename}")

    print(f"\nĐã tạo {len(created)} file .drawio trong: {OUT_DIR}")


if __name__ == "__main__":
    main()
