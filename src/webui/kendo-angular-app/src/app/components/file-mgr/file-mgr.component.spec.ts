import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileMgrComponent } from './file-mgr.component';

describe('FileMgrComponent', () => {
  let component: FileMgrComponent;
  let fixture: ComponentFixture<FileMgrComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FileMgrComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FileMgrComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
