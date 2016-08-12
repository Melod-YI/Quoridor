package model.impl;

import static org.junit.Assert.*;

import org.junit.Before;
import org.junit.Test;

import abstracter.Direction;

public class ChessBoardModelImplTest {

	@Before
	public void setUp() throws Exception {
	}

	@Test
	public void test() {
		ChessBoardModelImpl cbm = new ChessBoardModelImpl();
		assertTrue(cbm.initialize(9, 9, 10, 3));
//		assertTrue(cbm.move(4, Direction.down));
//		assertTrue(cbm.move(1, Direction.down));
//		assertTrue(cbm.move(1, Direction.left));
		cbm.blockPrint();
	}

}
