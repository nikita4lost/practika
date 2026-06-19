from __future__ import annotations

import json
from dataclasses import dataclass, asdict
from datetime import datetime
from pathlib import Path

from kivy.app import App
from kivy.metrics import dp
from kivy.uix.boxlayout import BoxLayout
from kivy.uix.button import Button
from kivy.uix.label import Label
from kivy.uix.popup import Popup
from kivy.uix.scrollview import ScrollView
from kivy.uix.spinner import Spinner
from kivy.uix.textinput import TextInput


STATUSES = [
    "Принято",
    "На диагностике",
    "Ремонт",
    "Возврат средств/Обмен",
    "Отказ",
]


@dataclass
class WarrantyCase:
    Id: str
    ClientName: str
    Phone: str
    ProductName: str
    SerialNumber: str
    Reason: str
    Status: str
    CheckSum: float
    ReceivedAt: str
    RequiresSupplierApproval: bool
    ManagerComment: str
    TechnicalConclusion: str
    UpdatedAt: str


class CaseStore:
    def __init__(self) -> None:
        self.cloud_file = Path(__file__).resolve().parents[1] / "cloud" / "warranty_cases.json"
        self.cloud_file.parent.mkdir(parents=True, exist_ok=True)

    def load(self) -> list[WarrantyCase]:
        if not self.cloud_file.exists():
            return []
        data = json.loads(self.cloud_file.read_text(encoding="utf-8"))
        return [WarrantyCase(**item) for item in data]

    def save(self, cases: list[WarrantyCase]) -> None:
        payload = [asdict(item) for item in cases]
        self.cloud_file.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


class CaseCard(BoxLayout):
    def __init__(self, item: WarrantyCase, open_callback, **kwargs) -> None:
        super().__init__(orientation="vertical", padding=dp(12), spacing=dp(6), size_hint_y=None, height=dp(145), **kwargs)
        self.item = item
        self.open_callback = open_callback

        self.add_widget(Label(text=f"[b]{item.Id}[/b]  {item.ProductName}", markup=True, size_hint_y=None, height=dp(26)))
        self.add_widget(Label(text=f"SN: {item.SerialNumber} | Статус: {item.Status}", size_hint_y=None, height=dp(24)))
        self.add_widget(Label(text=f"Причина: {item.Reason}", size_hint_y=None, height=dp(24)))
        self.add_widget(Label(text=f"Комментарий: {item.ManagerComment or '-'}", size_hint_y=None, height=dp(24)))

        button = Button(text="Открыть заявку", size_hint_y=None, height=dp(36))
        button.bind(on_press=lambda *_: self.open_callback(item))
        self.add_widget(button)


class MainScreen(BoxLayout):
    def __init__(self, **kwargs) -> None:
        super().__init__(orientation="vertical", padding=dp(14), spacing=dp(10), **kwargs)
        self.store = CaseStore()
        self.cases: list[WarrantyCase] = []

        title = Label(
            text="[b]Сервисный центр - гарантия[/b]\\nроль: engineer",
            markup=True,
            size_hint_y=None,
            height=dp(58),
        )
        self.add_widget(title)

        filters = BoxLayout(size_hint_y=None, height=dp(44), spacing=dp(8))
        self.status_filter = Spinner(text="Все статусы", values=["Все статусы", *STATUSES])
        self.status_filter.bind(text=lambda *_: self.refresh())
        reload_button = Button(text="Загрузить")
        reload_button.bind(on_press=lambda *_: self.load_cases())
        filters.add_widget(self.status_filter)
        filters.add_widget(reload_button)
        self.add_widget(filters)

        self.scroll = ScrollView()
        self.list_box = BoxLayout(orientation="vertical", spacing=dp(10), size_hint_y=None)
        self.list_box.bind(minimum_height=self.list_box.setter("height"))
        self.scroll.add_widget(self.list_box)
        self.add_widget(self.scroll)

        self.load_cases()

    def load_cases(self) -> None:
        self.cases = self.store.load()
        self.refresh()

    def refresh(self) -> None:
        self.list_box.clear_widgets()
        status = self.status_filter.text
        items = self.cases
        if status != "Все статусы":
            items = [item for item in items if item.Status == status]

        if not items:
            self.list_box.add_widget(Label(text="Заявок не найдено", size_hint_y=None, height=dp(44)))
            return

        for item in items:
            self.list_box.add_widget(CaseCard(item, self.open_case))

    def open_case(self, item: WarrantyCase) -> None:
        layout = BoxLayout(orientation="vertical", padding=dp(12), spacing=dp(8))
        layout.add_widget(Label(text=f"[b]{item.Id}[/b]", markup=True, size_hint_y=None, height=dp(30)))
        layout.add_widget(Label(text=f"Клиент: {item.ClientName}", size_hint_y=None, height=dp(24)))
        layout.add_widget(Label(text=f"Товар: {item.ProductName}", size_hint_y=None, height=dp(24)))
        layout.add_widget(Label(text=f"SN: {item.SerialNumber}", size_hint_y=None, height=dp(24)))
        layout.add_widget(Label(text=f"Причина: {item.Reason}", size_hint_y=None, height=dp(24)))

        status_spinner = Spinner(text=item.Status, values=STATUSES, size_hint_y=None, height=dp(44))
        conclusion_input = TextInput(
            text=item.TechnicalConclusion,
            hint_text="Техническое заключение",
            multiline=True,
            size_hint_y=None,
            height=dp(110),
        )
        layout.add_widget(status_spinner)
        layout.add_widget(conclusion_input)

        buttons = BoxLayout(size_hint_y=None, height=dp(44), spacing=dp(8))
        save_button = Button(text="Сохранить")
        close_button = Button(text="Закрыть")
        buttons.add_widget(save_button)
        buttons.add_widget(close_button)
        layout.add_widget(buttons)

        popup = Popup(title="Карточка заявки", content=layout, size_hint=(0.92, 0.86))

        def save_case(*_) -> None:
            item.Status = status_spinner.text
            item.TechnicalConclusion = conclusion_input.text.strip()
            item.UpdatedAt = datetime.now().isoformat(timespec="seconds")
            self.store.save(self.cases)
            self.refresh()
            popup.dismiss()

        save_button.bind(on_press=save_case)
        close_button.bind(on_press=lambda *_: popup.dismiss())
        popup.open()


class WarrantyEngineerApp(App):
    def build(self) -> MainScreen:
        self.title = "Сервисный центр - гарантия"
        return MainScreen()


if __name__ == "__main__":
    WarrantyEngineerApp().run()
