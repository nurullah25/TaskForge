import { CdkDrag, CdkDragHandle } from '@angular/cdk/drag-drop';
import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { BoardColumn } from '../board.models';

@Component({
  selector: 'app-board-column',
  imports: [CdkDrag, CdkDragHandle, MatIconModule, MatButtonModule, MatMenuModule],
  templateUrl: './board-column.html',
  styleUrl: './board-column.scss',
  host: { class: 'board-column' },
})
export class BoardColumnComponent {
  readonly column = input.required<BoardColumn>();
  readonly canManage = input(false);
  readonly taskCount = input(0);

  readonly edit = output<BoardColumn>();
  readonly remove = output<BoardColumn>();
}
