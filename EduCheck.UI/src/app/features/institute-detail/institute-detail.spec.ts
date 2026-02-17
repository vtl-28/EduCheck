import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InstituteDetail } from './institute-detail';

describe('InstituteDetail', () => {
  let component: InstituteDetail;
  let fixture: ComponentFixture<InstituteDetail>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InstituteDetail]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InstituteDetail);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
